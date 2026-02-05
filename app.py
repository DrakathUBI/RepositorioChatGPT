from __future__ import annotations

import os
import re
import unicodedata
from pathlib import Path
from typing import Iterable, List

from flask import Flask, flash, redirect, render_template, request, send_file, url_for
from pypdf import PdfReader, PdfWriter

try:
    from pdf2image import convert_from_path
    import pytesseract
except Exception:  # optional OCR dependencies
    convert_from_path = None
    pytesseract = None

BASE_DIR = Path(__file__).resolve().parent
UPLOAD_DIR = BASE_DIR / "uploads"
OUTPUT_DIR = BASE_DIR / "output"

UPLOAD_DIR.mkdir(exist_ok=True)
OUTPUT_DIR.mkdir(exist_ok=True)

app = Flask(__name__)
app.config["MAX_CONTENT_LENGTH"] = 50 * 1024 * 1024
app.secret_key = os.getenv("FLASK_SECRET_KEY", "dev-secret")


NAME_PATTERN = re.compile(r"\b([A-ZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ][a-záàâãéèêíìîóòôõúùûç]+(?:\s+[A-ZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ][a-záàâãéèêíìîóòôõúùûç]+)+)\b")


def clean_filename(name: str) -> str:
    normalized = unicodedata.normalize("NFKD", name)
    normalized = "".join(ch for ch in normalized if not unicodedata.combining(ch))
    normalized = re.sub(r"[^\w\-\s,]", "", normalized)
    normalized = re.sub(r"\s+", " ", normalized).strip()
    return normalized.replace(" ", "_") or "aluno_desconhecido"


def extract_text_from_pdf(pdf_path: Path) -> str:
    reader = PdfReader(str(pdf_path))
    all_text = []
    for page in reader.pages:
        page_text = page.extract_text() or ""
        if page_text.strip():
            all_text.append(page_text)
    if all_text:
        return "\n".join(all_text)

    if convert_from_path and pytesseract:
        images = convert_from_path(str(pdf_path), dpi=250, first_page=1, last_page=3)
        return "\n".join(pytesseract.image_to_string(img, lang="por") for img in images)

    return ""


def discover_names(text: str, roster: Iterable[str] | None = None) -> List[str]:
    text = text.replace("\n", " ")

    if roster:
        matches = []
        lowered = text.lower()
        for student in roster:
            if student.lower() in lowered:
                matches.append(student)
        if matches:
            return sorted(set(matches), key=matches.index)

    possible = [match.strip() for match in NAME_PATTERN.findall(text)]
    filtered = [name for name in possible if len(name.split()) <= 4]

    if not filtered:
        return ["Aluno não identificado"]

    unique_names = []
    for name in filtered:
        if name not in unique_names:
            unique_names.append(name)

    return unique_names[:5]


def split_pages_by_student(source_pdf: Path, names: List[str]) -> list[Path]:
    reader = PdfReader(str(source_pdf))
    results = []

    if len(names) == 1:
        target = OUTPUT_DIR / f"{clean_filename(names[0])}.pdf"
        writer = PdfWriter()
        for page in reader.pages:
            writer.add_page(page)
        with target.open("wb") as f:
            writer.write(f)
        return [target]

    total_pages = len(reader.pages)
    chunk_size = max(1, total_pages // len(names))

    page_index = 0
    for i, student in enumerate(names):
        target = OUTPUT_DIR / f"{clean_filename(student)}.pdf"
        writer = PdfWriter()

        end = total_pages if i == len(names) - 1 else min(total_pages, page_index + chunk_size)
        while page_index < end:
            writer.add_page(reader.pages[page_index])
            page_index += 1

        with target.open("wb") as f:
            writer.write(f)
        results.append(target)

    return results


@app.get("/")
def index():
    return render_template("index.html")


@app.post("/processar")
def processar():
    pdf = request.files.get("pdf")
    roster_input = request.form.get("lista_alunos", "")

    if not pdf or not pdf.filename.lower().endswith(".pdf"):
        flash("Envie um arquivo PDF válido.", "error")
        return redirect(url_for("index"))

    roster = [name.strip() for name in roster_input.split(",") if name.strip()]

    temp_pdf_path = UPLOAD_DIR / pdf.filename
    pdf.save(temp_pdf_path)

    text = extract_text_from_pdf(temp_pdf_path)
    if not text.strip():
        flash(
            "Não foi possível extrair texto do PDF. Instale OCR (Tesseract + Poppler) para PDFs escaneados.",
            "error",
        )
        return redirect(url_for("index"))

    names = discover_names(text, roster=roster)
    generated = split_pages_by_student(temp_pdf_path, names)

    rendered_names = ", ".join(names)
    flash(f"Alunos detectados: {rendered_names}", "success")

    return render_template("result.html", files=[file.name for file in generated], names=names)


@app.get("/download/<path:filename>")
def download(filename: str):
    return send_file(OUTPUT_DIR / filename, as_attachment=True)


if __name__ == "__main__":
    app.run(debug=True, host="0.0.0.0", port=5000)
