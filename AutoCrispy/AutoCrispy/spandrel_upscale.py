"""Folder-based RealPLKSR inference bridge for AutoCrispy.

Loads the 4x-PBRify_RPLKSRd_V3 checkpoint through Spandrel (the same model
loader family used by chaiNNer), then upscales the supported files in one
input directory. The model is kept resident for the whole batch.
"""

from __future__ import annotations

import argparse
import sys
import traceback
from pathlib import Path
from typing import Any


SUPPORTED_EXTENSIONS = {
    ".png",
    ".jpg",
    ".jpeg",
    ".bmp",
    ".tif",
    ".tiff",
    ".webp",
    ".tga",
}
EXPECTED_MODEL_NAME = "4x-PBRify_RPLKSRd_V3.pth"
MODEL_SCALE = 4
TILE_OVERLAP = 32


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Upscale a folder of textures with 4x-PBRify_RPLKSRd_V3."
    )
    parser.add_argument("model", type=Path, help="RealPLKSR .pth checkpoint")
    parser.add_argument("--input", required=True, type=Path, help="Input folder")
    parser.add_argument("--output", required=True, type=Path, help="Output folder")
    parser.add_argument(
        "--tile-size",
        type=int,
        default=512,
        help="Maximum input tile edge in pixels (default: 512)",
    )
    parser.add_argument(
        "--cpu", action="store_true", help="Force CPU inference instead of CUDA"
    )
    parser.add_argument(
        "--debug", action="store_true", help="Print a full traceback on errors"
    )
    return parser.parse_args()


def _load_model(model_path: Path, force_cpu: bool) -> tuple[Any, Any, Any, str]:
    try:
        import torch
        from spandrel import ImageModelDescriptor, ModelLoader
    except ImportError as error:
        raise RuntimeError(
            "RealPLKSR needs PyTorch and Spandrel. Follow PLKSR_SETUP.md "
            "to install them in the Python environment AutoCrispy uses."
        ) from error

    if model_path.name.casefold() != EXPECTED_MODEL_NAME.casefold():
        raise ValueError(f"Only {EXPECTED_MODEL_NAME} is supported by this AutoCrispy runner.")
    if not model_path.is_file():
        raise FileNotFoundError(f"Checkpoint not found: {model_path}")

    device = torch.device(
        "cpu" if force_cpu or not torch.cuda.is_available() else "cuda:0"
    )
    descriptor = ModelLoader(device).load_from_file(model_path)
    if not isinstance(descriptor, ImageModelDescriptor):
        raise ValueError("The selected checkpoint is not an image super-resolution model.")

    tags = {str(tag).casefold() for tag in descriptor.tags}
    if (
        descriptor.scale != MODEL_SCALE
        or descriptor.input_channels != 3
        or descriptor.output_channels != 3
        or "real" not in tags
        or "dysample" not in tags
    ):
        raise ValueError(
            "Expected the 4x RealPLKSR-DySample RGB checkpoint "
            f"{EXPECTED_MODEL_NAME}; Spandrel detected scale={descriptor.scale}, "
            f"channels={descriptor.input_channels}->{descriptor.output_channels}, "
            f"tags={sorted(tags)}."
        )

    # Some RealPLKSR-DySample checkpoints do not support half precision. Respect
    # Spandrel's per-checkpoint capability instead of forcing fp16 on the GPU.
    dtype = (
        torch.float16
        if device.type == "cuda" and descriptor.supports_half
        else torch.float32
    )
    descriptor.to(device, dtype)
    descriptor.model.eval()
    return descriptor, device, dtype, str(device)


def _infer_tile(model: Any, image: Any, device: Any, dtype: Any) -> Any:
    import numpy as np
    import torch

    tensor = torch.from_numpy(np.ascontiguousarray(image.transpose(2, 0, 1)))
    tensor = tensor.unsqueeze(0).to(device=device, dtype=dtype)
    with torch.inference_mode():
        result = model(tensor)
    return result.squeeze(0).permute(1, 2, 0).float().cpu().numpy()


def _tile_starts(length: int, core_size: int) -> list[int]:
    if length <= core_size:
        return [0]
    return list(range(0, length, core_size))


def _upscale_rgb(
    image: Any, model: Any, device: Any, dtype: Any, tile_size: int
) -> Any:
    import numpy as np

    height, width, _ = image.shape
    scale = int(model.scale)
    if tile_size <= 0 or (height <= tile_size and width <= tile_size):
        return _infer_tile(model, image, device, dtype)

    overlap = min(TILE_OVERLAP, max(8, tile_size // 8))
    core_size = max(1, tile_size - 2 * overlap)
    y_starts = _tile_starts(height, core_size)
    x_starts = _tile_starts(width, core_size)
    output = np.empty((height * scale, width * scale, 3), dtype=np.float32)

    for core_y0 in y_starts:
        core_y1 = min(core_y0 + core_size, height)
        input_y0 = max(0, core_y0 - overlap)
        input_y1 = min(height, core_y1 + overlap)
        for core_x0 in x_starts:
            core_x1 = min(core_x0 + core_size, width)
            input_x0 = max(0, core_x0 - overlap)
            input_x1 = min(width, core_x1 + overlap)

            tile_output = _infer_tile(
                model,
                image[input_y0:input_y1, input_x0:input_x1, :],
                device,
                dtype,
            )
            out_y0 = (core_y0 - input_y0) * scale
            out_x0 = (core_x0 - input_x0) * scale
            out_y1 = out_y0 + (core_y1 - core_y0) * scale
            out_x1 = out_x0 + (core_x1 - core_x0) * scale
            output[
                core_y0 * scale : core_y1 * scale,
                core_x0 * scale : core_x1 * scale,
                :,
            ] = tile_output[out_y0:out_y1, out_x0:out_x1, :]

    return output


def _is_out_of_memory(error: RuntimeError) -> bool:
    return "out of memory" in str(error).casefold() or "cuda error: out of memory" in str(
        error
    ).casefold()


def _upscale_with_fallback(
    image: Any, model: Any, device: Any, dtype: Any, tile_size: int
) -> Any:
    import torch

    current_tile_size = max(64, tile_size)
    while True:
        try:
            return _upscale_rgb(image, model, device, dtype, current_tile_size)
        except RuntimeError as error:
            if not _is_out_of_memory(error) or current_tile_size <= 64:
                raise
            current_tile_size = max(64, current_tile_size // 2)
            if device.type == "cuda":
                torch.cuda.empty_cache()
            print(
                f"GPU memory was insufficient; retrying with {current_tile_size}px tiles.",
                file=sys.stderr,
            )


def _process_image(
    input_path: Path,
    output_path: Path,
    model: Any,
    device: Any,
    dtype: Any,
    tile_size: int,
) -> None:
    import numpy as np
    from PIL import Image, ImageOps

    with Image.open(input_path) as source:
        source.load()
        source = ImageOps.exif_transpose(source)
        has_alpha = "A" in source.getbands() or "transparency" in source.info
        icc_profile = source.info.get("icc_profile")

        if has_alpha:
            rgba = np.asarray(source.convert("RGBA"), dtype=np.uint8)
            rgb = rgba[:, :, :3]
            alpha = Image.fromarray(rgba[:, :, 3])
        else:
            rgb = np.asarray(source.convert("RGB"), dtype=np.uint8)
            alpha = None

    rgb_float = rgb.astype(np.float32) / 255.0
    upscaled = _upscale_with_fallback(rgb_float, model, device, dtype, tile_size)
    upscaled = np.clip(np.rint(upscaled * 255.0), 0, 255).astype(np.uint8)
    expected_size = (rgb.shape[1] * MODEL_SCALE, rgb.shape[0] * MODEL_SCALE)

    if upscaled.shape[:2] != (expected_size[1], expected_size[0]):
        raise ValueError(
            f"Unexpected output dimensions for {input_path.name}: "
            f"{upscaled.shape[1]}x{upscaled.shape[0]} instead of "
            f"{expected_size[0]}x{expected_size[1]}."
        )

    if alpha is not None:
        resampling = getattr(Image, "Resampling", Image).LANCZOS
        alpha = alpha.resize(expected_size, resampling)
        result = Image.fromarray(np.dstack((upscaled, np.asarray(alpha))))
    else:
        result = Image.fromarray(upscaled)

    save_options: dict[str, Any] = {}
    if icc_profile:
        save_options["icc_profile"] = icc_profile
    if input_path.suffix.casefold() in {".jpg", ".jpeg"}:
        result = result.convert("RGB")
        save_options.update(quality=95, subsampling=0)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    result.save(output_path, **save_options)


def run(args: argparse.Namespace) -> int:
    if not args.input.is_dir():
        raise NotADirectoryError(f"Input folder not found: {args.input}")
    args.output.mkdir(parents=True, exist_ok=True)

    model, device, dtype, device_name = _load_model(args.model, args.cpu)
    print(
        f"Loaded {args.model.name}: RealPLKSR-DySample x{model.scale} on "
        f"{device_name} ({dtype})."
    )

    files = sorted(
        path
        for path in args.input.iterdir()
        if path.is_file() and path.suffix.casefold() in SUPPORTED_EXTENSIONS
    )
    for input_path in files:
        _process_image(
            input_path,
            args.output / input_path.name,
            model,
            device,
            dtype,
            args.tile_size,
        )

    print(f"Upscaled {len(files)} image(s).")
    return 0


def main() -> int:
    args = parse_args()
    try:
        return run(args)
    except Exception as error:
        print(f"AutoCrispy RealPLKSR error: {error}", file=sys.stderr)
        if args.debug:
            traceback.print_exc()
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
