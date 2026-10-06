# Spandrel upscalers: PLKSR, DAT2, and generic checkpoints

AutoCrispy can run the fixed PBRify 4× models and compatible checkpoints recognized by Spandrel. These are optional texture upscalers, not the full PBRify material workflow: AutoCrispy does not generate normal, roughness, or AO maps, and it does not need the chaiNNer GUI. Checkpoint weights and the Python/PyTorch runtime are installed separately; do not add model weights to the Git repository.

- **PLKSR** — `4x-PBRify_RPLKSRd_V3.pth` (RealPLKSR-DySample)
- **DAT2** — `4x-PBRify_UpscalerV4.pth` (DAT architecture / DAT2 model)
- **Spandrel** — any checkpoint recognized by Spandrel as a 4× RGB super-resolution model

## 1. Install checkpoints

Place checkpoints in the configured backend folder, beside AutoCrispy, or beneath the program folder. The fixed PLKSR and DAT2 backends are detected by their exact `.pth` filenames. Example layout:

```text
Backend/
  PLKSR/
    4x-PBRify_RPLKSRd_V3.pth
  DAT2/
    4x-PBRify_UpscalerV4.pth
  Spandrel/
    my-4x-model.safetensors
    another-model.pth
    collection/
      model.ckpt
```

Download the fixed checkpoints from their releases:

- [4x-PBRify_RPLKSRd_V3](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_RPLKSRd_V3)
- [4x-PBRify_UpscalerV4](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_UpscalerV4)

The optional [PLKSR source ZIP](https://github.com/dslisleedh/PLKSR) is not executed by AutoCrispy. Use the `.pth` asset from the V4 release. The PLKSR and DAT2 checkpoints are independent; install either or both.

For generic checkpoints, use a folder named **`Spandrel`** directly under the configured backend folder (or directly under AutoCrispy's program folder). The selector checks `.pth`, `.pt`, `.ckpt`, and `.safetensors` files in that folder and up to three nested folders. Each candidate is loaded through Spandrel; only image super-resolution models with scale 4 and three input and output channels appear in the selector. Unsupported, corrupt, non-SR, non-4×, or non-RGB checkpoints are skipped. The legacy **ESRGAN** selector remains separate and unchanged.

## 2. Install the inference runtime

Install **Python 3.10 or newer**. The selected Python must be on `PATH`, or `python.exe` must be beside AutoCrispy (or in a `python/` subfolder). If it is installed elsewhere, set the `AUTOCRISPY_PYTHON` environment variable to the full path to `python.exe`.

Install a PyTorch build appropriate for the computer. For NVIDIA GPU acceleration, first use the official [PyTorch install selector](https://pytorch.org/get-started/locally/) to install the CUDA-enabled build. Then, in that same Python environment, run:

```bat
python -m pip install "spandrel==0.4.2" Pillow
```

Spandrel 0.4.2 detects RealPLKSR-DySample, DAT architectures, and other supported architectures. AutoCrispy validates model scale, purpose, and channels before inference. If CUDA is unavailable, it falls back to CPU; CPU processing will be much slower. The generic selector validates candidate files on startup, so checking a folder containing many large checkpoints can take a little time.

## 3. Use a Spandrel model

1. Start AutoCrispy. The exact PLKSR and DAT2 checkpoints appear as separate backends when installed. The generic **Spandrel** backend appears when at least one eligible checkpoint is found in the `Spandrel` folder.
2. If PLKSR and DAT2 are both installed, PLKSR remains the default; DAT2 is preferred over generic Spandrel when PLKSR is absent. If only generic Spandrel models are found, that backend is selected. Choose a model from its selector, then add it from the chain tab if needed.
3. Start the watcher as usual. The helper loads the selected checkpoint once per batch, then processes new textures.

A saved chain entry using the exact legacy model `4x_gameai_2.0` is upgraded to PLKSR when it is installed, otherwise to DAT2 if that is the available fixed PBRify backend. Other ESRGAN chains are left alone.

The tile-size setting controls the maximum inference tile size. Start at `512`; increase it for fewer tiles/faster processing if there is enough GPU memory, or lower it if inference runs out of memory. Set it to `0` to try processing each image without tiling; on an out-of-memory error, AutoCrispy retries with 512-pixel tiles and then smaller tiles automatically. The CPU checkbox forces CPU inference. **DAT2 V4 is substantially slower and more memory-intensive than the PLKSR V3 option**, so tiled inference is recommended for large textures.

These are RGB upscalers, not normal/roughness/AO generators. If an input has transparency, the runner upscales RGB and resizes its alpha channel separately with Lanczos. Use AutoCrispy's existing alpha/defringe settings as needed.
