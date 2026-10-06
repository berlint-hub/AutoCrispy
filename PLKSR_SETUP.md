# PBRify PLKSR and DAT2 setup

AutoCrispy can run two fixed 4× PBRify texture upscalers through its bundled Spandrel helper:

- **PLKSR** — `4x-PBRify_RPLKSRd_V3.pth` (RealPLKSR-DySample)
- **DAT2** — `4x-PBRify_UpscalerV4.pth` (DAT architecture / DAT2 model)

These are optional live upscalers, not the full PBRify material workflow. AutoCrispy does not generate normal, roughness, or AO maps and does not need the chaiNNer GUI. The checkpoint and Python/PyTorch runtime are installed separately; do not add model weights to the Git repository.

## 1. Install a checkpoint

Place the checkpoint beside your other backend folders. For the same layout as the ESRGAN backend, you can download the optional [PLKSR source ZIP](https://github.com/dslisleedh/PLKSR), extract it, and rename its folder to `PLKSR`; AutoCrispy does not execute those source files. For example:

```text
Backend/
  ESRGAN/
    esrgan.exe
    models/...
  PLKSR/
    (optional extracted PLKSR source files)
    4x-PBRify_RPLKSRd_V3.pth
  DAT2/
    4x-PBRify_UpscalerV4.pth
```

Download the exact `.pth` checkpoint you want to use from its release:

- [4x-PBRify_RPLKSRd_V3](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_RPLKSRd_V3)
- [4x-PBRify_UpscalerV4](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_UpscalerV4)

AutoCrispy searches the configured backend folder and its nested folders, then the program folder, for each exact filename. The PLKSR source ZIP is optional; AutoCrispy does not execute that repository. Use the `.pth` asset from the V4 release. The two checkpoints are independent; install either or both.

When detected, **PLKSR** and/or **DAT2** appear as separate entries in the backend dropdown. Their model selector shows the fixed checkpoint that was found. If both are installed, PLKSR remains the default; choose **DAT2** to use V4.

## 2. Install the inference runtime

Install **Python 3.10 or newer**. The selected Python must be on `PATH`, or `python.exe` must be beside AutoCrispy (or in a `python/` subfolder). If it is installed elsewhere, set the `AUTOCRISPY_PYTHON` environment variable to the full path to `python.exe`.

Install a PyTorch build appropriate for the computer. For NVIDIA GPU acceleration, first use the official [PyTorch install selector](https://pytorch.org/get-started/locally/) to install the CUDA-enabled build. Then, in that same Python environment, run:

```bat
python -m pip install "spandrel==0.4.2" Pillow
```

Spandrel 0.4.2 detects RealPLKSR-DySample and DAT architectures, so DAT2 V4 does not need a separate architecture package. AutoCrispy checks the detected scale, channels, and architecture before inference. If CUDA is unavailable, AutoCrispy falls back to CPU; CPU processing will be much slower.

## 3. Use it in AutoCrispy

1. Start AutoCrispy and choose **PLKSR** or **DAT2** in the backend dropdown. If both are installed, PLKSR is preselected; choosing DAT2 leaves PLKSR available as an alternative.
2. A saved chain entry using the exact legacy model `4x_gameai_2.0` is upgraded to PLKSR when it is installed, otherwise to DAT2 if that is the available PBRify backend. Other ESRGAN chains are left alone. If your chain is empty or still uses another model, add the desired backend from the chain tab.
3. Start the watcher as usual. The helper loads the selected checkpoint once per batch, then processes the new textures.

The tile-size setting controls the maximum inference tile size. Start at `512`; increase it for fewer tiles/faster processing if there is enough GPU memory, or lower it if inference runs out of memory. Set it to `0` to try processing each image without tiling; on an out-of-memory error, AutoCrispy retries with 512-pixel tiles and then smaller tiles automatically. The CPU checkbox forces CPU inference. **DAT2 V4 is substantially slower and more memory-intensive than the PLKSR V3 option**, so tiled inference is recommended for large textures.

Both checkpoints are RGB 4× upscalers, not normal/roughness/AO generators. If an input has transparency, the runner upscales RGB and resizes its alpha channel separately with Lanczos. Use AutoCrispy's existing alpha/defringe settings as needed.
