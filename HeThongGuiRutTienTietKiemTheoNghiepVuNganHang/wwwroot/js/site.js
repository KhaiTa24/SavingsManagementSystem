// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Xử lý hiển thị/ẩn form thêm tài liệu
document.addEventListener('DOMContentLoaded', function() {
    const addButton = document.getElementById('add-document-button');
    const addForm = document.getElementById('add-document-form');
    const cancelButton = document.getElementById('cancel-add-button');

    if (addButton && addForm && cancelButton) {
        addButton.addEventListener('click', function() {
            addForm.classList.remove('hidden');
        });

        cancelButton.addEventListener('click', function() {
            addForm.classList.add('hidden');
        });
    }

    // Xử lý form thêm tài liệu
    const addFormInner = document.getElementById('add-form-inner');
    if (addFormInner) {
        addFormInner.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const formData = {
                title: document.getElementById('title').value,
                author: document.getElementById('author').value,
                format: document.getElementById('format').value,
                size: document.getElementById('size').value
            };

            // Gọi API để thêm tài liệu
            addDocument(formData);
        });
    }

    // Load danh sách tài liệu
    loadDocuments();
});

// Hàm thêm tài liệu
async function addDocument(formData) {
    try {
        const response = await fetch('/api/documents', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            alert('Thêm tài liệu thành công!');
            document.getElementById('add-document-form').classList.add('hidden');
            loadDocuments(); // Tải lại danh sách
        } else {
            alert('Có lỗi xảy ra khi thêm tài liệu.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Có lỗi xảy ra khi thêm tài liệu.');
    }
}

// Hàm tải danh sách tài liệu
async function loadDocuments() {
    try {
        const response = await fetch('/api/documents');
        const documents = await response.json();
        
        const documentList = document.getElementById('document-list');
        if (documentList) {
            documentList.innerHTML = documents.map(doc => `
                <div class="document-card">
                    <img src="${doc.thumbnailUrl || '/images/default-document.png'}" 
                         alt="${doc.title}" 
                         class="document-image">
                    <div class="document-info">
                        <h3 class="document-title">${doc.title}</h3>
                        <div class="document-meta">
                            <span>${doc.author}</span> • 
                            <span>${doc.format}</span> • 
                            <span>${doc.size}MB</span>
                        </div>
                        <p class="document-description">${doc.description}</p>
                        <div class="flex justify-between items-center">
                            <button class="btn btn-primary" onclick="downloadDocument(${doc.id})">
                                Tải xuống
                            </button>
                            <button class="btn btn-secondary" onclick="viewDetails(${doc.id})">
                                Chi tiết
                            </button>
                        </div>
                    </div>
                </div>
            `).join('');
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

// Hàm tải tài liệu
async function downloadDocument(documentId) {
    try {
        const response = await fetch(`/api/documents/${documentId}/download`);
        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = ''; // Tên file sẽ được lấy từ response
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();
        } else {
            alert('Không thể tải tài liệu.');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Có lỗi xảy ra khi tải tài liệu.');
    }
}

// Hàm xem chi tiết tài liệu
function viewDetails(documentId) {
    window.location.href = `/DocumentsView/Details/${documentId}`;
}

// Xử lý tìm kiếm
const searchInput = document.querySelector('input[type="text"]');
if (searchInput) {
    searchInput.addEventListener('input', debounce(function(e) {
        const searchTerm = e.target.value;
        searchDocuments(searchTerm);
    }, 300));
}

// Hàm tìm kiếm tài liệu
async function searchDocuments(searchTerm) {
    try {
        const response = await fetch(`/api/documents/search?q=${encodeURIComponent(searchTerm)}`);
        const documents = await response.json();
        updateDocumentList(documents);
    } catch (error) {
        console.error('Error:', error);
    }
}

// Hàm cập nhật danh sách tài liệu
function updateDocumentList(documents) {
    const documentList = document.getElementById('document-list');
    if (documentList) {
        documentList.innerHTML = documents.map(doc => `
            <div class="document-card">
                <img src="${doc.thumbnailUrl || '/images/default-document.png'}" 
                     alt="${doc.title}" 
                     class="document-image">
                <div class="document-info">
                    <h3 class="document-title">${doc.title}</h3>
                    <div class="document-meta">
                        <span>${doc.author}</span> • 
                        <span>${doc.format}</span> • 
                        <span>${doc.size}MB</span>
                    </div>
                    <p class="document-description">${doc.description}</p>
                    <div class="flex justify-between items-center">
                        <button class="btn btn-primary" onclick="downloadDocument(${doc.id})">
                            Tải xuống
                        </button>
                        <button class="btn btn-secondary" onclick="viewDetails(${doc.id})">
                            Chi tiết
                        </button>
                    </div>
                </div>
            </div>
        `).join('');
    }
}

// Hàm debounce để tránh gọi API quá nhiều lần
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}
