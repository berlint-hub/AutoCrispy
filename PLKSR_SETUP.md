# RealPLKSR / 4x-PBRify setup

AutoCrispy can run **only the 4x-PBRify_RPLKSRd_V3 upscaler checkpoint** as a live folder-watching backend. It does not need the complete PBRify material-generation workflow or the chaiNNer GUI. The inference bridge uses [Spandrel](https://github.com/chaiNNer-org/spandrel), the PyTorch model loader used by chaiNNer.

## 1. Get the checkpoint

Download `4x-PBRify_RPLKSRd_V3.pth` from the [model release](https://github.com/Kim2091/Kim2091-Models/releases/tag/4x-PBRify_RPLKSRd_V3). Do not add the checkpoint to the AutoCrispy Git repository.

Put the file in the AutoCrispy program folder or the configured backend search root, or in a folder up to two levels below either location. For example:

```text
AutoCrispy.exe
spandrel_upscale.py
Models/
  4x-PBRify_RPLKSRd_V3.pth
```

The file name must remain `4x-PBRify_RPLKSRd_V3.pth`. AutoCrispy will show **RealPLKSR (Spandrel)** in the backend list when it finds the checkpoint.

## 2. Install the inference runtime

Install **Python 3.10 or newer**. The selected Python must be on `PATH`, or `python.exe` must be beside AutoCrispy (or in a `python/` subfolder). If it is installed elsewhere, set the `AUTOCRISPY_PYTHON` environment variable to the full path to `python.exe`.

Install a PyTorch build appropriate for the computer. For NVIDIA GPU acceleration, first use the official [PyTorch install selector](https://pytorch.org/get-started/locally/) to install the CUDA-enabled build. Then, in that same Python environment, run:

```bat
python -m pip install "spandrel==0.4.2" Pillow
```

Spandrel 0.4.2 includes PLKSR and RealPLKSR DySample checkpoint detection. If CUDA is unavailable, AutoCrispy falls back to CPU; CPU processing will be slower.

## 3. Use it in AutoCrispy

1. Start AutoCrispy. When the checkpoint is found, **RealPLKSR (Spandrel)** is preselected as the live upscaler.
2. An existing chain entry that uses the exact legacy model `4x_gameai_2.0` is upgraded to **RealPLKSR 4x** automatically. Other ESRGAN chains are left alone. If your chain is empty or still uses another model, add **RealPLKSR 4x** from the chain tab and remove the old upscaler if appropriate. The checkpoint is fixed to the PBRify model; the model drop-down is intentionally not a general model picker.
3. Start the watcher as usual. The runner loads the checkpoint once per batch, then processes the new textures.

The existing tile-size setting controls the maximum inference tile size. Start at `512`; increase it for fewer tiles/faster processing if there is enough GPU memory, or lower it if inference runs out of memory. CUDA out-of-memory errors trigger smaller-tile retries automatically. The CPU checkbox forces CPU inference.

The checkpoint is an RGB 4x upscaler, not a normal/roughness/AO generator. If an input has transparency, the runner upscales RGB with RealPLKSR and resizes its alpha channel separately with Lanczos. Use AutoCrispy's existing alpha/defringe settings as needed.
