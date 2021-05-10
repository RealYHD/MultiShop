function dragAndDropList(elem) {
    elem.addEventListener("dragover", (e) => {
        e.preventDefault();
    });
    let itemDragged;
    for (let i = 0; i < elem.childElementCount; i++) {
        let e = elem.children[i];
        e.addEventListener("dragstart", () => {
            itemDragged = e;
            e.classList.add("list-group-item-secondary");
        });
        e.addEventListener("dragenter", () => {
            e.classList.add("list-group-item-primary");
        });
        e.addEventListener("dragleave", () => {
            e.classList.remove("list-group-item-primary");
        });
        e.addEventListener("drop", () => {
            e.classList.remove("list-group-item-primary");
            itemDragged.classList.remove("list-group-item-secondary");
        });
    }
}