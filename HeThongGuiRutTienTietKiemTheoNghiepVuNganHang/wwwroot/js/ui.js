// Đối tượng quản lý giao diện
const DocumentUI = {
    // Tham chiếu đến các phần tử DOM (khởi tạo ban đầu là null)
    elements: {
        documentList: null,
        documentCount: null,
        addForm: null,
        editForm: null
    },

    // Khởi tạo giao diện
    initialize() {
        this.elements.documentList = document.getElementById('document-list');
        this.elements.documentCount = document.getElementById('document-count');
        this.elements.addForm = document.getElementById('add-document-form');

        this.setupEventListeners();
        this.loadDocuments();
    },

    // Thiết lập các sự kiện listener
    setupEventListeners() {
        document.getElementById('add-document-button').addEventListener('click', () => this.showAddForm());
        document.getElementById('add-form-inner').addEventListener('submit', (e) => this.handleAddFormSubmit(e));
        document.getElementById('cancel-add-button').addEventListener('click', () => this.hideAddForm());
        this.elements.documentList.addEventListener('click', (e) => this.handleDocumentListClick(e));
    },

    // Tải danh sách tài liệu
    async loadDocuments() {
        const documents = await DocumentAPI.getAll();
        this.updateDocumentCount(documents.length);
        this.renderDocumentList(documents);
    },

    // Cập nhật số lượng tài liệu
    updateDocumentCount(count) {
        this.elements.documentCount.textContent = `XEM TẤT CẢ TÀI LIỆU: ${count}`;
    },

    // Hiển thị danh sách tài liệu
    renderDocumentList(documents) {
        const documentList = this.elements.documentList;
        documentList.innerHTML = '';

        if (documents.length === 0) {
            documentList.innerHTML = '<div class="p-4 text-center text-gray-500">Không có tài liệu nào.</div>';
            return;
        }

        documents.forEach((doc, index) => {
            const docElement = this.createDocumentElement(doc, index);
            documentList.appendChild(docElement);
        });
    },

    // Tạo phần tử HTML cho một tài liệu
    createDocumentElement(doc, index) {
        const div = document.createElement('div');
        div.className = `p-4 rounded-md shadow-sm flex items-center justify-between ${index % 2 === 0 ? 'bg-white' : 'bg-gray-50'}`;
        div.dataset.docId = doc.id;

        const infoDiv = document.createElement('div');
        infoDiv.className = 'flex items-center space-x-4';

        const icon = document.createElement('i');
        const iconClass = this.getIconClass(doc.format);
        icon.className = `${iconClass} text-blue-500 text-2xl`;
        infoDiv.appendChild(icon);

        const textDiv = document.createElement('div');

        const title = document.createElement('h3');
        title.className = 'text-blue-600 font-medium';
        title.textContent = doc.title;
        textDiv.appendChild(title);

        const details = document.createElement('p');
        details.className = 'text-gray-500 text-sm';
        details.textContent = `${this.formatDate(doc.uploadDate)} | ${doc.format} | ${doc.size} MB | 👁️ ${doc.views} | ⬇ ${doc.downloads}`;
        textDiv.appendChild(details);

        infoDiv.appendChild(textDiv);
        div.appendChild(infoDiv);

        const buttonDiv = document.createElement('div');
        buttonDiv.className = 'space-x-2';

        buttonDiv.appendChild(this.createButton('CHI TIẾT', 'bg-blue-500 hover:bg-blue-600', 'view-button'));
        buttonDiv.appendChild(this.createButton('TẢI VỀ MÁY', 'bg-blue-500 hover:bg-blue-600', 'download-button'));
        buttonDiv.appendChild(this.createButton('SỬA', 'bg-yellow-500 hover:bg-yellow-600', 'edit-button'));
        buttonDiv.appendChild(this.createButton('XÓA', 'bg-red-500 hover:bg-red-600', 'delete-button'));

        div.appendChild(buttonDiv);
        return div;
    },

    // Tạo nút chức năng
    createButton(text, bgClass, className) {
        const button = document.createElement('button');
        button.textContent = text;
        button.className = `${bgClass} text-white px-4 py-2 rounded-md ${className}`;
        return button;
    },

    // Xử lý sự kiện click trên danh sách tài liệu
    async handleDocumentListClick(event) {
        const target = event.target;
        if (target.tagName !== 'BUTTON') return;

        const docElement = target.closest('div[data-doc-id]');
        if (!docElement) return;

        const docId = docElement.dataset.docId;

        // Kiểm tra giá trị docId
        if (!docId || isNaN(docId)) {
            alert('ID tài liệu không hợp lệ. Vui lòng thử lại.');
            return;
        }

        // Chuyển docId thành số nguyên
        const parsedDocId = parseInt(docId);

        if (target.classList.contains('view-button')) {
            await this.viewDocument(parsedDocId);
        } else if (target.classList.contains('download-button')) {
            this.downloadDocument(parsedDocId);
        } else if (target.classList.contains('edit-button')) {
            await this.editDocument(parsedDocId);
        } else if (target.classList.contains('delete-button')) {
            await this.deleteDocument(parsedDocId);
        }
    },

    // Hiển thị form thêm tài liệu
    showAddForm() {
        this.elements.addForm.classList.remove('hidden');
    },

    // Ẩn form thêm tài liệu
    hideAddForm() {
        const addFormContainer = document.getElementById('add-document-form');
        const addForm = document.getElementById('add-form-inner');
        if (addForm) {
            addForm.reset();
        }
        if (addFormContainer) {
            addFormContainer.classList.add('hidden');
        }
    },

    // Xử lý sự kiện submit form thêm tài liệu
    async handleAddFormSubmit(event) {
        event.preventDefault();

        const newDoc = {
            title: document.getElementById('title').value.trim(),
            author: document.getElementById('author').value.trim(),
            format: document.getElementById('format').value,
            size: parseFloat(document.getElementById('size').value),
            views: 0,
            downloads: 0
        };

        const response = await DocumentAPI.create(newDoc);
        if (response === null) {
            alert("Tài liệu đã được thêm nhưng không nhận được phản hồi từ API.");
        } else {
            alert("Thêm tài liệu thành công!");
        }

        this.hideAddForm();
        await this.loadDocuments();
    },

    // Xem chi tiết tài liệu
    async viewDocument(id) {
        window.location.href = `/DocumentsView/Details/${id}`;
    },

    // Tải tài liệu
    downloadDocument(id) {
        alert(`Đang tải tài liệu có ID: ${id}`);
    },

    // Hiển thị form chỉnh sửa tài liệu
    async editDocument(id) {
        const doc = await DocumentAPI.getById(id);
        this.createEditForm(doc);

        const editFormElement = document.getElementById('edit-document-form');
        if (editFormElement) {
            editFormElement.classList.remove('hidden');
            editFormElement.scrollIntoView({ behavior: 'smooth' });
        }
    },

    // Tạo form chỉnh sửa tài liệu
    createEditForm(doc) {
        const oldForm = document.getElementById('edit-document-form');
        if (oldForm && oldForm.parentNode) {
            oldForm.parentNode.removeChild(oldForm);
        }

        const formContainer = document.createElement('div');
        formContainer.id = 'edit-document-form';
        formContainer.className = 'bg-white p-4 rounded-md shadow-sm mb-4';

        formContainer.innerHTML = `
        <h3 class="text-lg font-bold mb-4">Chỉnh Sửa Tài Liệu</h3>
        <form id="edit-form">
            <input type="hidden" id="edit-id" value="${doc.id}">
            
            <div class="mb-4">
                <label class="block text-sm font-medium mb-1">Tiêu đề:</label>
                <input type="text" id="edit-title" value="${doc.title}" class="w-full py-2 px-4 border border-gray-300 rounded-md" required>
            </div>
            
            <div class="mb-4">
                <label class="block text-sm font-medium mb-1">Tác giả:</label>
                <input type="text" id="edit-author" value="${doc.author}" class="w-full py-2 px-4 border border-gray-300 rounded-md" required>
            </div>
            
            <div class="mb-4">
                <label class="block text-sm font-medium mb-1">Định dạng:</label>
                <select id="edit-format" class="w-full py-2 px-4 border border-gray-300 rounded-md" required>
                    <option value="PPTX" ${doc.format === 'PPTX' ? 'selected' : ''}>PPTX</option>
                    <option value="DOCX" ${doc.format === 'DOCX' ? 'selected' : ''}>DOCX</option>
                    <option value="PDF" ${doc.format === 'PDF' ? 'selected' : ''}>PDF</option>
                </select>
            </div>

            <div class="mb-4">
                <label class="block text-sm font-medium mb-1">Kích thước (MB):</label>
                <input type="number" id="edit-size" value="${doc.size}" class="w-full py-2 px-4 border border-gray-300 rounded-md">
            </div>

            <!-- Ẩn lượt xem và lượt tải -->
            <div class="hidden">
                <input type="hidden" id="edit-views" value="${doc.views}">
            </div>
            
            <div class="hidden">
                <input type="hidden" id="edit-downloads" value="${doc.downloads}">
            </div>

            <div class="flex space-x-2">
                <button type="submit" class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-md">Cập nhật</button>
                <button type="button" id="cancel-edit-button" class="bg-gray-500 hover:bg-gray-600 text-white px-4 py-2 rounded-md">Hủy</button>
            </div>
        </form>
    `;

        const mainElement = document.querySelector('main');
        if (mainElement) {
            mainElement.appendChild(formContainer);

            setTimeout(() => {
                const editFormElement = document.getElementById('edit-form');
                const cancelButton = document.getElementById('cancel-edit-button');

                if (editFormElement) {
                    editFormElement.addEventListener('submit', (e) => this.handleEditFormSubmit(e));
                }

                if (cancelButton) {
                    cancelButton.addEventListener('click', () => this.hideEditForm());
                }
            }, 0);
        }
    },

    // Ẩn form chỉnh sửa
    hideEditForm() {
        const editForm = document.getElementById('edit-document-form');
        if (editForm) {
            editForm.classList.add('hidden');
        }
    },

    // Xử lý sự kiện submit form chỉnh sửa
    async handleEditFormSubmit(event) {
        event.preventDefault();

        const formElement = document.getElementById('edit-form');
        if (!formElement) return;

        const idElement = document.getElementById('edit-id');
        const titleElement = document.getElementById('edit-title');
        const authorElement = document.getElementById('edit-author');
        const formatElement = document.getElementById('edit-format');
        const sizeElement = document.getElementById('edit-size');
        const viewsElement = document.getElementById('edit-views');
        const downloadsElement = document.getElementById('edit-downloads');

        if (!idElement || !titleElement || !authorElement || !formatElement || !sizeElement || !viewsElement || !downloadsElement) return;

        const id = idElement.value;
        const updatedDoc = {
            title: titleElement.value,
            author: authorElement.value,
            format: formatElement.value,
            size: parseFloat(sizeElement.value),
            views: parseInt(viewsElement.value),
            downloads: parseInt(downloadsElement.value)
        };

        await DocumentAPI.update(id, updatedDoc);
        this.hideEditForm();
        await this.loadDocuments();
        alert('Cập nhật tài liệu thành công!');
    },

    // Xóa tài liệu
    async deleteDocument(id) {
        if (confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) {
            await DocumentAPI.delete(id);
            await this.loadDocuments();
            alert('Xóa tài liệu thành công!');
        }
    },

    // Các hàm tiện ích
    getIconClass(format) {
        format = format.toLowerCase();
        if (format === 'pptx') return 'fas fa-file-powerpoint';
        if (format === 'docx') return 'fas fa-file-word';
        return 'fas fa-file-pdf';
    },

    formatDate(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('vi-VN');
    }
};

// Khởi tạo giao diện khi tài liệu đã tải xong
document.addEventListener('DOMContentLoaded', () => {
    DocumentUI.initialize();
});