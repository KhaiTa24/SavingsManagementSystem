//API port
const baseUrl = 'http://localhost:5062';

const DocumentAPI = {
    // GET
    async getAll() {
        const response = await fetch(`${baseUrl}/api/documents`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (!response.ok) {
            throw new Error(`Lỗi API: ${response.status}`);
        }
        return await response.json();
    },

    // GET by ID
    async getById(id) {
        const response = await fetch(`${baseUrl}/api/documents/${id}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (!response.ok) {
            throw new Error(`Lỗi API: ${response.status}`);
        }
        return await response.json();
    },

    // POST
    async create(document) {
        const response = await fetch(`${baseUrl}/api/documents`, {
            method: 'POST',
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(document)
        });

        if (!response.ok) {
            throw new Error(`Lỗi API: ${response.status} - ${response.statusText}`);
        }

        const text = await response.text();
        if (!text) {
            return null;
        }

        return JSON.parse(text);
    },

    // PUT
    async update(id, document) {
        const response = await fetch(`${baseUrl}/api/documents/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(document)
        });

        if (!response.ok) {
            throw new Error(`Lỗi API: ${response.status} - ${response.statusText}`);
        }

        const contentType = response.headers.get("Content-Type");
        if (!contentType || !contentType.includes("application/json")) {
            return null;
        }

        return await response.json();
    },

    // DELETE
    async delete(id) {
        const response = await fetch(`${baseUrl}/api/documents/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error(`Lỗi API: ${response.status}`);
        }
        return true;
    }
};

// Export API để sử dụng trong file khác
window.DocumentAPI = DocumentAPI;