#!/usr/bin/env python3
"""
export_onnx.py
Экспортирует CNN1D из best_model.pth в ONNX-формат.
Сохраняет параметры нормализации в scalers.json.
Запускается из OnnxModelExporter.cs через Process.Start.
"""

import argparse
import json
import sys
import traceback
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
FAULT_DISTANCE_DIR = SCRIPT_DIR.parent.parent / "Fault-Distance"
if FAULT_DISTANCE_DIR.exists():
    sys.path.insert(0, str(FAULT_DISTANCE_DIR))


def export(pth_path: str, output_dir: str) -> dict:
    import torch
    from models.cnn1d import CNN1D

    pth_path = Path(pth_path)
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    checkpoint = torch.load(pth_path, map_location="cpu", weights_only=False)
    cfg = checkpoint.get("config", None)
    scalers = checkpoint.get("scalers", {})

    if cfg is None:
        raise RuntimeError("В checkpoint нет сохранённого config")

    model = CNN1D(
        seq_length=int(cfg.SEQ_LENGTH),
        num_channels=int(cfg.NUM_CHANNELS),
        num_filters=int(cfg.NUM_FILTERS),
        kernel_size=int(cfg.KERNEL_SIZE),
        dropout=float(cfg.DROPOUT),
    )
    model.load_state_dict(checkpoint["model_state_dict"])
    model.eval()

    seq_len = int(cfg.SEQ_LENGTH)
    num_ch = int(cfg.NUM_CHANNELS)

    dummy_input = torch.zeros(1, num_ch, seq_len)

    onnx_path = output_dir / "best_model.onnx"
    torch.onnx.export(
        model,
        dummy_input,
        str(onnx_path),
        input_names=["input"],
        output_names=["output"],
        dynamic_axes={
            "input": {0: "batch_size"},
            "output": {0: "batch_size"},
        },
        opset_version=14,
    )

    signal_scalers = scalers.get("signal", [])
    dist_scaler = scalers.get("distance", None)

    signal_means = []
    signal_stds = []
    for scaler in signal_scalers:
        if hasattr(scaler, "mean_"):
            signal_means.append(float(scaler.mean_[0]))
            signal_stds.append(float(scaler.scale_[0]))
        elif hasattr(scaler, "mean"):
            signal_means.append(float(scaler.mean))
            signal_stds.append(float(scaler.scale_))

    dist_min = 0.0
    dist_max = float(cfg.LINE_L_KM) if hasattr(cfg, "LINE_L_KM") else 50.0
    if dist_scaler is not None:
        if hasattr(dist_scaler, "data_min_"):
            dist_min = float(dist_scaler.data_min_[0])
            dist_max = float(dist_scaler.data_max_[0])
        elif hasattr(dist_scaler, "min_"):
            dist_min = float(dist_scaler.min_[0])
            dist_max = float(dist_scaler.max_[0])

    scalers_out = {
        "signal_means": signal_means,
        "signal_stds": signal_stds,
        "dist_min": dist_min,
        "dist_max": dist_max,
        "num_channels": num_ch,
        "seq_length": seq_len,
        "normalization_mode": str(getattr(cfg, "NORMALIZATION_MODE", "standard")),
    }

    scalers_path = output_dir / "scalers.json"
    with open(scalers_path, "w", encoding="utf-8") as f:
        json.dump(scalers_out, f, ensure_ascii=False, indent=2)

    return {
        "success": True,
        "onnx_path": str(onnx_path),
        "scalers_path": str(scalers_path),
        "channels": num_ch,
        "seq_length": seq_len,
    }


def main():
    parser = argparse.ArgumentParser(description="Export PyTorch model to ONNX")
    parser.add_argument("--input", required=True, help="Путь к best_model.pth")
    parser.add_argument("--output", required=True, help="Выходная директория")
    args = parser.parse_args()

    try:
        result = export(args.input, args.output)
        print(json.dumps(result))
        sys.exit(0)
    except Exception as e:
        error_info = {
            "success": False,
            "error": str(e),
            "traceback": traceback.format_exc(),
        }
        print(json.dumps(error_info))
        sys.exit(1)


if __name__ == "__main__":
    main()