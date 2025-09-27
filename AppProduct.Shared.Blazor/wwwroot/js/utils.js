// Utility functions for Blazor application

// Download file from base64 data
window.downloadFile = function (filename, contentType, base64Data) {
    const byteArray = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([byteArray], { type: contentType });
    
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    
    // Trigger download
    document.body.appendChild(link);
    link.click();
    
    // Clean up
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

// Print current page
window.printPage = function () {
    window.print();
};