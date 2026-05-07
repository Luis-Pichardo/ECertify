document.addEventListener('DOMContentLoaded', function () {
    const uploadDiv = document.getElementById('xmlUpload');
    const inputFile = document.getElementById('archivoXml');
    const fileNameDiv = document.getElementById('fileNameXml');
    const form = document.getElementById('uploadForm');
    const submitBtn = document.getElementById('submitBtn');

    inputFile.addEventListener('change', function () {
        if (this.files.length > 0) {
            const file = this.files[0];
            if (!file.name.toLowerCase().endsWith('.xml')) {
                fileNameDiv.textContent = 'Archivo no válido. Solo se permite .xml';
                fileNameDiv.classList.add('text-danger');
                return;
            }
            fileNameDiv.textContent = file.name;
            fileNameDiv.classList.remove('text-danger');
        } else {
            fileNameDiv.textContent = '';
        }
    });
});