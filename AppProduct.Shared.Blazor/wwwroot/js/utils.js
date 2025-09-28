// Utility functions for Blazor application

// Download file from base64 data
window.downloadFile = function (filename, contentType, base64Data) {
    const byteArray = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([byteArray], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Print current page
window.printPage = function () { window.print(); };

// Ensure we extend any existing mudElementRef object instead of overwriting it
window.mudElementRef = window.mudElementRef || {};
(function(api){
    const helpers = {
        addOnBlurEvent: function (element, dotNetObjectRef, methodName) {
            if (element?.addEventListener) {
                element.addEventListener('blur', e => dotNetObjectRef?.invokeMethodAsync?.(methodName, e));
            }
        },
        addOnFocusEvent: function (element, dotNetObjectRef, methodName) {
            if (element?.addEventListener) {
                element.addEventListener('focus', e => dotNetObjectRef?.invokeMethodAsync?.(methodName, e));
            }
        },
        addOnKeyDownEvent: function (element, dotNetObjectRef, methodName) {
            if (element?.addEventListener) {
                element.addEventListener('keydown', e => dotNetObjectRef?.invokeMethodAsync?.(methodName, e.key, e.ctrlKey, e.shiftKey, e.altKey));
            }
        },
        addOnKeyUpEvent: function (element, dotNetObjectRef, methodName) {
            if (element?.addEventListener) {
                element.addEventListener('keyup', e => dotNetObjectRef?.invokeMethodAsync?.(methodName, e.key, e.ctrlKey, e.shiftKey, e.altKey));
            }
        },
        focus: function (element) { element?.focus?.(); },
        select: function (element) { element?.select?.(); },
        blur: function (element) { element?.blur?.(); },
        getBoundingClientRect: function (element) {
            if (!element || !element.getBoundingClientRect) return null;
            const r = element.getBoundingClientRect();
            return {
                x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, bottom: r.bottom, left: r.left, right: r.right,
                X: r.x, Y: r.y, Width: r.width, Height: r.height, Top: r.top, Bottom: r.bottom, Left: r.left, Right: r.right
            };
        }
    };
    for (const k in helpers){ if(!api[k]) api[k] = helpers[k]; }
})(window.mudElementRef);