# Renomeador de PDF por Aluno (OCR)

Site em Flask para enviar PDFs, detectar nomes de alunos via texto/OCR e gerar arquivos renomeados.

## Como executar

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python app.py
```

Acesse: `http://localhost:5000`

## Dependências OCR para PDF escaneado

Além do `pip install`, você precisa:

- **Tesseract OCR** (com idioma `por`)
- **Poppler** (usado por `pdf2image`)

Se o PDF já tiver texto selecionável, a extração funciona sem OCR.

## Fluxo

1. Envie um PDF.
2. Opcionalmente informe lista de alunos separada por vírgula (`Ata Lilian Katia, Pedro Russo`).
3. O sistema detecta os nomes e gera arquivos em `output/`.
