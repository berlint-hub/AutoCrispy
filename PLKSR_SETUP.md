# PLKSR / 4x-PBRify setup

AutoCrispy adds a **PLKSR** backend for the single `4x-PBRify_RPLKSRd_V3` 4× texture-upscaling checkpoint. It does not run the full PBRify material workflow or need the chaiNNer GUI. The checkpoint is a RealPLKSR-DySample model, loaded by the bundled AutoCrispy helper through [Spandrel](https://github.com/chaiNNer-org/spandrel).

## 1. Prepare the PLKSR folder and checkpoint

If you want the same folder layout as your ESRGAN backend, download the [PLKSR source ZIP](https://github.com/dslisleedh/PLKSR) and extract/rename its folder to `PLKSR` beside `ESRGAN`:

```text
Backend/
  ESRGAN/
    esrgan.exe
    models/...
  PLKSR/
    (the extracted PLKSR source files)
    4x-PBRify_RPLKSRd_V3.pth
```

The source ZIP is optional: AutoCrispy does not execute that repository, and it does not contain the PBRify checkpoint or a ready-to-run `plksr.exe`. Download the exact checkpoint separately from the [model release](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_RPLKSRd_V3) and put it directly in `PLKSR` or its `pretrained_models` subfolder. AutoCrispy searches the configured backend folder and the program folder for this exact filename, including nested folders. Do not add the checkpoint to the Git repository.

When found, **PLKSR** appears in AutoCrispy's backend list, and the model selector shows the fixed `4x-PBRify_RPLKSRd_V3.pth` checkpoint.

## 2. Install the inference runtime

Install **Python 3.10 or newer**. The selected Python must be on `PATH`, or `python.exe` must be beside AutoCrispy (or in a `python/` subfolder). If it is installed elsewhere, set the `AUTOCRISPY_PYTHON` environment variable to the full path to `python.exe`.

Install a PyTorch build appropriate for the computer. For NVIDIA GPU acceleration, first use the official [PyTorch install selector](https://pytorch.org/get-started/locally/) to install the CUDA-enabled build. Then, in that same Python environment, run:

```bat
python -m pip install "spandrel==0.4.2" Pillow
```

Spandrel 0.4.2 includes PLKSR and RealPLKSR DySample checkpoint detection. If CUDA is unavailable, AutoCrispy falls back to CPU; CPU processing will be slower.

## 3. Use it in AutoCrispy

1. Start AutoCrispy. When the checkpoint is found, **PLKSR** is preselected as the live backend; its model selector shows the PBRify checkpoint.
2. A saved chain entry using the exact legacy model `4x_gameai_2.0` is upgraded to **PLKSR 4x** automatically. Other ESRGAN chains are left alone. If your chain is empty or still uses another model, add **PLKSR 4x** from the chain tab and remove the old upscaler if appropriate. The model is intentionally fixed to this one checkpoint.
3. Start the watcher as usual. The runner loads the checkpoint once per batch, then processes the new textures.

The existing tile-size setting controls the maximum inference tile size. Start at `512`; increase it for fewer tiles/faster processing if there is enough GPU memory, or lower it if inference runs out of memory. Set it to `0` to try processing each image without tiling; if that runs out of memory, AutoCrispy retries with 512-pixel tiles and then smaller tiles automatically. The CPU checkbox forces CPU inference.

The checkpoint is an RGB 4x upscaler, not a normal/roughness/AO generator. If an input has transparency, the runner upscales RGB with RealPLKSR and resizes its alpha channel separately with Lanczos. Use AutoCrispy's existing alpha/defringe settings as needed.
