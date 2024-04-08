function blurElementAndDescendents(element) {
    element.blur();
    for (let descendent of element.querySelectorAll('*')) {
        if (descendent instanceof HTMLElement) {
            descendent.blur();
        }
    }
}

const maxTextAreaHeight = 250;

function textAreaExpandOnInput() {
    this.style.height = "auto";
    if (this.scrollHeight > maxTextAreaHeight) {
        this.style.height = maxTextAreaHeight + 'px';
        this.style.overflowY = 'auto';
    } else {
        this.style.height = this.scrollHeight + "px";
        this.style.overflowY = 'hidden';
    }
}

function textAreaPreventNewlines(event) {
    if (event.key === 'Enter') {
        event.preventDefault();
        return false;
    }
}

// Add feature for providing a element id as a Dropdown clipping boundary
bootstrap.Dropdown.Default.popperConfig = ((config) => {
    const preventOverflow = config.modifiers.filter(modifier => modifier.name === 'preventOverflow')[0];
    let boundary = preventOverflow.options.boundary;
    if (typeof boundary === 'string' && boundary !== 'clippingParents' && boundary !== 'viewport' && boundary !== 'document') {
        boundary = document.getElementById(boundary);
        preventOverflow.options.padding = 2;
    }
    preventOverflow.options.boundary = boundary;
    
    return config;
});

function elementAddedCallback(mutations) {
    for (let mutation of mutations) {
        for (let node of mutation.addedNodes) {
            if (!(node instanceof HTMLElement)) continue;

            if (node.getAttribute('data-bs-toggle') === "tooltip") {
                new bootstrap.Tooltip(node);
                node.addEventListener("click", function() {
                    bootstrap.Tooltip.getInstance(this).hide();
                });
            }

            if (node.classList.contains('autofocus')) {
                node.focus();
            }

            if (node.classList.contains('text-area-no-newlines')) {
                node.addEventListener('keydown', textAreaPreventNewlines);
            }

            if (node.classList.contains('text-area-expand')) {
                if (node.tagName.toLowerCase() !== 'textarea') {
                    console.warn('"text-area-expand" is being applied to element type "' + node.tagName + '" instead of "textarea"');
                }
                textAreaExpandOnInput.call(node);
                node.addEventListener('input', textAreaExpandOnInput);
            }

            if (node.classList.contains('scroll-to-in-modal')) {
                scrollModalToElement(node);
            }
        }
    }
}

const observer = new MutationObserver(elementAddedCallback);
observer.observe(document, {
    subtree: true,
    childList: true,
    characterData: true,
});

function makeSortable(dotnet, root, listKey, handle, groupKey) {
    root.listKey = listKey;
    Sortable.create(root, {
        group: groupKey,
        handle: handle,
        onEnd: function (evt) {
            dotnet.invokeMethodAsync("onEnd", {
                from: evt.from.listKey,
                to: evt.to.listKey,
                oldIndex: evt.oldIndex,
                newIndex: evt.newIndex,
                oldDraggableIndex: evt.oldDraggableIndex,
                newDraggableIndex: evt.newDraggableIndex,
        });
        }
    });
}

function scrollToTop() {
    document.body.scrollIntoView({behavior: 'smooth', block: 'start'});
}

function scrollModalToElement(element) {
    let container = document.getElementById("ModalRootContainer");
    container.scrollTop = element.offsetTop;
    element.classList.add("glowing-border-fade-out");
}

function scrollToElement(element) {
    element.scrollIntoView({behavior: 'smooth', block: 'start'});
}

function windowMatchMedia(mediaString) {
    return window.matchMedia(mediaString).matches;
}

function updateTooltip(elem, target) {
    let popper = elem.popper
    let boundaryElem = elem.closest('.tooltip-clipping-parent');

    if (popper === undefined) {
        popper = Popper.createPopper(target, elem, {
            modifiers: [
                {
                    name: 'preventOverflow',
                    options: {
                        boundary: boundaryElem || 'clippingParents',
                    }
                },
            ]
        });
        elem.popper = popper;
    } else {
        popper.state.elements.reference = target;
        popper.update();
    }
}

function ToggleCommitDropdown() {
    document.getElementById("worklog-commits-button").classList.toggle("show");
    document.getElementById("commits-dropdown").classList.toggle("show");
}

function getDocumentUrl()
{
    return document.URL;
}

function highlightDescendants(elem)
{
    elem.querySelectorAll('pre code')
        .forEach((el) => hljs.highlightElement(el));
}

function addClassToElement(id, className)
{
    document.getElementById(id).classList.add(className);
}

function removeClassFromElement(id, className)
{
    document.getElementById(id).classList.remove(className);
}

function applyTutorialBlurring(parentContainerId)
{
    const container = document.querySelector('#' + parentContainerId);
    const allDescendants = container.getElementsByTagName('*');

    for (const element of allDescendants) {
        if (!hasAncestorWithClass(element, 'spotlight-section-active') 
            && !hasAncestorWithClass(element, 'blur')
            && !hasDescendantWithClass(element, 'spotlight-section-active')
            && !element.classList.contains('spotlight-section-active')) 
        {
            element.classList.add('blur');
        } else {
            element.classList.remove('blur');
        }
    }
}

function hasAncestorWithClass(element, className) {
    let currentElement = element.parentElement;

    while (currentElement) {
        if (currentElement.classList.contains(className)) {
            return true;
        }
        currentElement = currentElement.parentElement;
    }

    return false;
}

function hasDescendantWithClass(element, className) {
    const descendants = element.getElementsByTagName('*');

    for (const descendant of descendants) {
        if (descendant.classList.contains(className)) {
            return true;
        }
    }

    return false;
}

function clickElement(element) {
    element.click();
}

function bindSlideNumberLabelForBootstrapCarousel(carouselId, labelId) {
    if(!document.getElementById(carouselId)) return;
    document.getElementById(carouselId).addEventListener('slid.bs.carousel', function() {
        let activeItem = document.querySelector(`#${carouselId} .carousel-item.active`);
        let items = Array.from(document.querySelectorAll(`#${carouselId} .carousel-item`));
        let currentIndex = items.indexOf(activeItem);
        document.getElementById(labelId).innerText = (currentIndex + 1).toString();
    });
}

let instance;
function markTextInsideElement(containerId, text) {
    if(instance) instance.unmark();
    instance = new Mark(document.querySelector("#" + containerId));
    instance.mark(text, {
        "acrossElements": true,
        "separateWordSearch": false,
        "accuracy": "exactly",
        "ignorePunctuation": ":;.,-–—‒_(){}[]!'\"+=".split(""),
        "each": (el) => window.scrollToElement(el)
    });
}

function setTooltipHtml(tooltipId, htmlContentContainerId) {
    let element = document.getElementById(tooltipId);
    if (!element) {
        console.error('Element with id ' + tooltipId + ' not found.');
        return;
    }

    let htmlContentContainer = document.getElementById(htmlContentContainerId);
    if (!htmlContentContainer) {
        console.error('Element with id ' + htmlContentContainerId + ' not found.');
        return;
    }

    // Destroy existing tooltip instance if it exists
    let tooltipInstance = bootstrap.Tooltip.getInstance(element);
    if (tooltipInstance) {
        tooltipInstance.dispose();
    }

    // Set new title
    element.setAttribute('data-bs-title', htmlContentContainer.innerHTML);

    // Re-initialize the tooltip with the new content
    let tooltip = new bootstrap.Tooltip(element, {html: true});
}
