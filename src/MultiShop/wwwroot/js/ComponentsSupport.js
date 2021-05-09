function customDropdown(elem, justify) {
    let btn = elem.querySelector("button");
    let dropdown = elem.querySelector("div");
    if (justify.toLowerCase() == "left") {
        dropdown.style.left = "0px";
    } else if (justify.toLowerCase() == "center") {
        dropdown.style.left = "50%";
    } else if (justify.toLowerCase() == "right") {
        dropdown.style.right = "0px";
    }

    let openFunc = () => {
        btn.classList.add("active");
        dropdown.classList.remove("invisible");
        dropdown.focus();
    }

    let closeFunc = () => {
        btn.classList.remove("active");
        dropdown.classList.add("invisible");
    }

    btn.addEventListener("click", () => {
        if (!btn.classList.contains("active")) {
            openFunc();
        } else {
            closeFunc();
        }
    });
    dropdown.addEventListener("focusout", (e) => {
        if (e.relatedTarget != btn) {
            closeFunc();
        }
    });
    dropdown.addEventListener("keyup", (e) => {
        if (e.code == "Escape") {
            dropdown.blur();
        }
    });
}

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
            e.classList.remove("list-group-item-hover");
        });
        e.addEventListener("dragenter", () => {
            e.classList.add("list-group-item-primary");
            e.classList.remove("list-group-item-hover");
        });
        e.addEventListener("dragleave", () => {
            e.classList.remove("list-group-item-primary");
            e.classList.add("list-group-item-hover");
        });
        e.addEventListener("drop", () => {
            e.classList.add("list-group-item-hover");
            e.classList.remove("list-group-item-primary");
            itemDragged.classList.remove("list-group-item-secondary");
            itemDragged.classList.add("list-group-item-hover");
        });
    }
}