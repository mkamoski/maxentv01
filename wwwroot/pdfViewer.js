window.pdfViewer = {
    createBlobUrl: function (byteArray) {
        const bytes = new Uint8Array(byteArray);
        const blob = new Blob([bytes], { type: 'application/pdf' });
        return URL.createObjectURL(blob);
    },
    revokeBlobUrl: function (url) {
        if (url) URL.revokeObjectURL(url);
    }
};
