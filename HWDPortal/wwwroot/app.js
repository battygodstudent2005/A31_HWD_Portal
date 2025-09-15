// 優化顯示 Modal 的函式
window.showBootstrapModal = function (modalId) {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
        modal.show();
    } else {
        console.error(`Modal with id '${modalId}' not found.`);
    }
}

// 隱藏 Bootstrap Modal 的函式
window.hideBootstrapModal = function (modalId) {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
        const modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    } else {
        console.error(`Modal with id '${modalId}' not found.`);
    }
}

// 複製文字到剪貼簿的函式
window.copyToClipboard = function (text) {
    navigator.clipboard.writeText(text).then(function () {
        console.log('路徑成功複製到剪貼簿!');
    }, function (err) {
        console.error('無法複製路徑: ', err);
        // 讓 C# 端來決定要顯示什麼錯誤訊息
        throw new Error('複製失敗!');
    });
}

// --- ★★★ 以下為本次新增的函式 ★★★ ---

/**
 * 顯示 Bootstrap Toast 通知
 * @param {string} message 要顯示的訊息
 * @param {string} status  通知的類型 ('success', 'danger', 'info' 等)
 */
window.showToast = function (message, status = 'info') {
    const toastContainer = document.querySelector('.toast-container');
    if (!toastContainer) {
        console.error('Toast container not found.');
        return;
    }

    // 根據狀態決定圖示和顏色
    const iconMap = {
        'success': 'bi-check-circle-fill',
        'danger': 'bi-exclamation-triangle-fill',
        'info': 'bi-info-circle-fill',
        'warning': 'bi-exclamation-triangle-fill'
    };
    const bgMap = {
        'success': 'text-bg-success',
        'danger': 'text-bg-danger',
        'info': 'text-bg-primary',
        'warning': 'text-bg-warning'
    }

    const iconClass = iconMap[status] || iconMap['info'];
    const bgClass = bgMap[status] || bgMap['info'];
    const toastId = 'toast-' + Math.random().toString(36).substring(2, 9);

    const toastHTML = `
        <div id="${toastId}" class="toast ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${iconClass} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>`;

    toastContainer.insertAdjacentHTML('beforeend', toastHTML);

    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        delay: 3500 // 通知顯示 3.5 秒
    });

    // 當 toast 關閉後，從 DOM 中移除它，避免元素堆積
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });

    toast.show();
}