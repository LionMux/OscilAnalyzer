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

    return {
        "success": True,
        "onnx_path": str(onnx_path),
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