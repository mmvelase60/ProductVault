document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".app-toast").forEach((element) => {
        bootstrap.Toast.getOrCreateInstance(element, { delay: 4500 }).show();
    });

    const imageInput = document.querySelector("[data-image-input]");
    const imagePreview = document.querySelector("[data-image-preview]");
    const previewContainer = document.querySelector("[data-image-preview-container]");

    imageInput?.addEventListener("change", () => {
        const [file] = imageInput.files;
        if (!file || !imagePreview || !previewContainer) return;

        imagePreview.src = URL.createObjectURL(file);
        previewContainer.classList.remove("d-none");
    });

    const categoryCode = document.querySelector("[data-category-code]");
    categoryCode?.addEventListener("input", () => {
        categoryCode.value = categoryCode.value.toUpperCase().replace(/[^A-Z0-9]/g, "");
    });
});
