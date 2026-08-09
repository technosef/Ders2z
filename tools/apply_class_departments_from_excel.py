from __future__ import annotations

import re
import shutil
import sqlite3
from datetime import datetime
from pathlib import Path

import openpyxl


DEFAULT_DB = Path.home() / "AppData" / "Local" / "DersDagitim" / "ders-dagitim.db"
DEFAULT_EXCEL = Path.home() / "Desktop" / "bilisim_meslek_sinif_listesi2025 i\u015fletme.xlsx"
BACKUP_DIR = Path(__file__).resolve().parents[1] / "backups"

DEPARTMENTS = {
    "B": "Bili\u015fim Teknolojileri",
    "E": "Elektrik-Elektronik Teknolojisi",
    "M": "Biyomedikal Cihaz Teknolojileri",
    "TB": "Bili\u015fim Teknolojileri",
    "TE": "Elektrik-Elektronik Teknolojisi",
    "TBM": "Biyomedikal Cihaz Teknolojileri",
}


def read_bilisim_classes(excel_path: Path) -> set[str]:
    workbook = openpyxl.load_workbook(excel_path, data_only=True)
    classes: set[str] = set()
    for sheet in workbook.worksheets:
        for row in sheet.iter_rows(values_only=True):
            for value in row:
                if not isinstance(value, str):
                    continue
                match = re.fullmatch(r"\s*(\d{1,2})\s*[-/]\s*([A-ZÇĞİÖŞÜ]+)\s*", value.strip(), re.I)
                if match:
                    classes.add(f"{match.group(1)}/{match.group(2).upper()}")
    return classes


def class_key(name: str) -> str:
    visible = re.sub(r"\s*\([^)]*\)", "", name).strip()
    return visible.replace("-", "/").replace(" ", "").upper()


def code_from_class(name: str, branch: str) -> str | None:
    match = re.search(r"\(([^)]+)\)", name)
    if match:
        return match.group(1).strip().upper()

    normalized_branch = branch.strip().upper()
    if normalized_branch.startswith("EE"):
        return "E"
    if normalized_branch.startswith("BM"):
        return "M"
    if re.fullmatch(r"[CDEFG]", normalized_branch):
        return "B"
    return None


def apply_departments(db_path: Path = DEFAULT_DB, excel_path: Path = DEFAULT_EXCEL) -> tuple[Path, int, int]:
    if not db_path.exists():
        raise FileNotFoundError(f"Veritabanı bulunamadı: {db_path}")
    if not excel_path.exists():
        raise FileNotFoundError(f"Ana Excel dosyası bulunamadı: {excel_path}")

    BACKUP_DIR.mkdir(exist_ok=True)
    backup_path = BACKUP_DIR / f"ders-dagitim-before-class-department-{datetime.now():%Y%m%d-%H%M%S}.db"
    shutil.copy2(db_path, backup_path)

    bilisim_classes = read_bilisim_classes(excel_path)
    updated = 0
    skipped = 0

    connection = sqlite3.connect(db_path)
    try:
        connection.execute("BEGIN")
        rows = connection.execute("SELECT Id,Name,Branch FROM SchoolClasses ORDER BY Grade,Branch,Name").fetchall()
        for class_id, name, branch in rows:
            code = code_from_class(name, branch)
            department = DEPARTMENTS.get(code or "")
            if class_key(name) in bilisim_classes:
                department = "Bili\u015fim Teknolojileri"
            if department is None:
                skipped += 1
                continue
            connection.execute("UPDATE SchoolClasses SET Department=? WHERE Id=?", (department, class_id))
            updated += 1
        connection.commit()
    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()

    return backup_path, updated, skipped


if __name__ == "__main__":
    backup, updated_count, skipped_count = apply_departments()
    print(f"backup={backup}")
    print(f"updated={updated_count}")
    print(f"skipped={skipped_count}")
