// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

let topBtn = document.getElementById("btn-back-to-top");
$(document).ready(function () {
    $(window).scroll(function () {
        if ($(this).scrollTop() > 100) {
            $('#btn-back-to-top').fadeIn();
        } else {
            $('#btn-back-to-top').fadeOut();
        }
    });
});

topBtn.addEventListener("click", toTop);

function toTop() {
    document.body.scrollTop = 0;
    document.documentElement.scrollTop = 0;
}

function PostSubmitHandler(e) {
    showSpinner();
    const form = this;
    e.preventDefault();

    let flag = false;

    const validation = document.querySelectorAll("span.text-danger");
    validation.forEach(span => {
        if (span.textContent != "") {
            showToast("Validation Error", span.textContent, false);
            console.log("Validation Error");
            flag = true;
        }
    });
    if (flag) { hideSpinner(); return; }
    console.log("Form Validated");
    fetch(form.action, {
        method: "POST",
        body: new FormData(form),
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
        .then(response => response.json())
        .then(data => {
            const modal = bootstrap.Modal.getInstance(document.getElementById("deleteModal"));
            if (modal) modal.hide();

            console.log(data);

            showToast(data.Title, data.Message, data.success);
        })
        .then(() => {
            document.querySelector("button.active").remove();
            document.getElementById("main-content").innerHTML = '<div class="h-100 d-flex justify-content-center align-items-center"><p>Select a feed...</p></div>';
            tabButtons = bindFeeds();
        })
        .then(() => {
            hideSpinner();
        })
        .catch(error => {
            console.log(error);
            showToast("Error Loading Page", "There was an error loading the page, please try again", false);
            hideSpinner();
        });
}

function bindDeleteModalHandlers() {
    console.log("Bind Delete Modal Handlers");
    document.querySelectorAll("button.delete").forEach(button => {
        console.log("Binding")
        button.removeEventListener("click", openDeleteModalHandler); // remove duplicates
        button.addEventListener("click", openDeleteModalHandler);
    });
}

function bindFormHandlers() {
    console.log("Bind Form Handlers");
    document.querySelectorAll(".form").forEach(form => {
        console.log("Binding")
        form.removeEventListener("submit", PostSubmitHandler); // remove duplicates
        form.addEventListener("submit", PostSubmitHandler);
    });
}


function openDeleteModalHandler(e) {
    const name = this.dataset.name;
    const url = this.dataset.url;

    console.log("Delete Modal Triggered\nName: " + name + "\nURL: " + url);

    if (name) { document.getElementById("deleteName").textContent = name; }
    document.getElementById("deleteForm").action = url;
}

function goBackOrRedirect(event, fallbackUrl) {
    event.preventDefault();
    console.log("goBackOrRedirect");
    if (document.referrer && document.referrer !== location.href) {
        history.back();
    } else {
        window.location.href = fallbackUrl;
    }
}

function showToast(title, message, isSuccess) {
    const date = Date.now();
    const toastId = `toast-${date}`;
    const toastHtml = `
                <div id="${toastId}" class="toast align-items-center text-bg-${isSuccess ? 'success' : 'danger'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                    <div class="d-flex">
                        <div class="toast-body">
                            <h5 class="toast-title">${title}</h5>
                            ${message}
                        </div>
                        <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                    </div>
                </div>
            `;

    const container = document.getElementById('toast-container');
    container.insertAdjacentHTML('beforeend', toastHtml);

    const toast = document.getElementById(toastId);
    const toastElement = new bootstrap.Toast(toast);
    toastElement.show();

    toast.addEventListener("hidden.bs.toast", () => {
        toast.remove();
    });
}

function showSpinner() {
    document.getElementById("main-spinner").classList.remove("d-none");
}

function hideSpinner() {
    document.getElementById("main-spinner").classList.add("d-none");
}

function getArticle(originalAction, pushState = true) {
    console.log("Get Article Triggered");

    fetch(originalAction, {
        method: "GET",
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            return response;
        })
        .then(response => response.text())
        .then(text => {
            let fail = "";
            try {
                json = JSON.parse(text);
                showToast(json.Title, json.Message, json.success); console.error(error);
                hideSpinner();
                fail = json.message;
            }
            catch (error) {
                try {
                    document.getElementById("main-content").innerHTML = text;
                    bindArticles();
                    bindDeleteModalHandlers();
                    bindFormHandlers()
                    if (pushState) { history.pushState(null, null, originalAction); }
                }
                catch (error) {
                    console.log(error);
                    throw new Error(error);
                }
            }

            if (fail !== "") {
                throw new Error(fail);
            }
        })
        .then(() => {
            hideSpinner();
        })
        .catch(error => {
            console.log(error);
            showToast("Error Loading Page", "There was an error loading the page, please try again", false);
            hideSpinner();
        });
}

function GetArticleHandler(e) {
    showSpinner();

    console.log("Get Handler Triggered");

    const originalAction = e.currentTarget.getAttribute("data-url");

    getArticle(originalAction);
}

function bindArticles() {
    const articles = document.querySelectorAll(".article-link");
    if (articles) {

        articles.forEach(button => {
            button.removeEventListener("click", GetArticleHandler);
            button.addEventListener("click", GetArticleHandler);
        });
    }
}

// Extract current tab from query param
function getCurrentTabFromURL() {
    const params = new URLSearchParams(window.location.search);
    return params.get("tab");
}

// Load tab content
function loadTab(button, tabButtons, updateHistory = true) {
    const url = button.getAttribute("data-url");
    const tab = url.split("/").pop().split("?")[0];
    const currentTab = getCurrentTabFromURL();

    // Avoid redundant load only if history *should* be updated
    if (tab === currentTab && updateHistory) {
        return;
    }

    showSpinner();

    fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
        .then(response => response.text())
        .then(text => {
            try {
                json = JSON.parse(text);
                showToast(json.Title, json.Message, json.success);
            }
            catch (error) {
                try {
                    document.getElementById("main-content").innerHTML = text;
                    bindArticles();
                    bindDeleteModalHandlers();
                    bindFormHandlers();
                    bindRefreshButtons();
                    bindReadToggles();
                    bindExternalReadButtons();
                    bindMarkAllRead();
                    if (updateHistory) {
                        history.pushState(null, null, window.location.pathname + "?tab=" + tab);
                    }
                    // Update tab UI
                    tabButtons.forEach(btn => btn.classList.remove("active"));
                    button.classList.add("active");
                }
                catch (error) {
                    console.log(error);
                    throw new Error(error);
                }
            }
        })
        .then(() => {
            hideSpinner();
        })
        .catch(error => {
            console.error(error);
            showToast("Error Loading Page", "There was an error loading the page, please try again", false);
            hideSpinner();
        });
}

function refreshFeed(button) {
    const url = button.getAttribute("data-url");
    showSpinner();

    fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" }
    })
        .then(response => response.text())
        .then(text => {
            try {
                json = JSON.parse(text);
                showToast(json.Title, json.Message, json.success);
            }
            catch (error) {
                try {
                    document.getElementById("main-content").innerHTML = text;
                    bindArticles();
                    bindDeleteModalHandlers();
                    bindFormHandlers()
                    bindRefreshButtons();
                    bindReadToggles();
                    bindExternalReadButtons();
                    bindMarkAllRead();
                }
                catch (error) {
                    console.log(error);
                    throw new Error(error);
                }
            }
        })
        .then(() => {
            hideSpinner();
        })
        .catch(error => {
            console.error(error);
            showToast("Error Loading Page", "There was an error loading the page, please try again", false);
            hideSpinner();
        });
}

function bindFeeds() {

    const tabButtons = document.querySelectorAll("[data-url]");
    // Handle user-initiated clicks
    tabButtons.forEach(button => {
        button.removeEventListener("click", function () {
            loadTab(this, tabButtons, true);
        });
        button.addEventListener("click", function () {
            loadTab(this, tabButtons, true);
        });
    });

    return tabButtons;
}

function bindRefreshButtons() {

    console.log("Bind Refresh Buttons");
    const refreshButtons = document.querySelectorAll(".refresh-button");
    refreshButtons.forEach(button => {
        console.log("Binding Refresh Button: " + button.getAttribute("data-url"));
        button.removeEventListener("click", function () {
            refreshFeed(this);
        });
        button.addEventListener("click", function () {
            refreshFeed(this);
        });
    });
}

// --- Read/Unread tracking ---

function toggleRead(entryId, button) {
    fetch(`/article/${entryId}/read`, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            const newIsRead = data.isRead;
            button.dataset.isRead = newIsRead.toString();
            button.title = newIsRead ? 'Mark unread' : 'Mark read';
            button.innerHTML = newIsRead ? '◉' : '○';

            const card = button.closest('.card');
            if (card) {
                card.classList.toggle('card-read', newIsRead);
                card.classList.toggle('card-unread', !newIsRead);
                const title = card.querySelector('.card-title');
                if (title) {
                    title.classList.toggle('fw-bold', !newIsRead);
                }
            }

            const feedId = getCurrentFeedId();
            if (feedId) {
                updateUnreadBadge(feedId, newIsRead ? -1 : 1);
            }
        }
    })
    .catch(error => {
        console.error('Toggle read error:', error);
    });
}

function handleExternalRead(button) {
    const entryId = button.dataset.entryId;
    const externalUrl = button.dataset.externalUrl;

    if (!entryId || !externalUrl) return;

    fetch(`/article/${entryId}/read`, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            // Update card styling
            const card = button.closest('.card');
            if (card) {
                card.classList.remove('card-unread');
                card.classList.add('card-read');
                const title = card.querySelector('.card-title');
                if (title) title.classList.remove('fw-bold');
                const toggleBtn = card.querySelector('.read-toggle');
                if (toggleBtn) {
                    toggleBtn.dataset.isRead = 'true';
                    toggleBtn.title = 'Mark unread';
                    toggleBtn.innerHTML = '◉';
                }
            }

            // Update feed badge
            const feedId = getCurrentFeedId();
            if (feedId) {
                updateUnreadBadge(feedId, -1);
            }

            // Open external URL
            window.open(externalUrl, '_blank');
        }
    })
    .catch(error => {
        console.error('External read error:', error);
    });
}

function markAllRead(feedId) {
    if (!feedId) return;

    fetch(`/feed/${feedId}/read`, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
    .then(r => r.json())
    .then(data => {
        if (data.success) {
            // Visually mark all cards as read
            document.querySelectorAll('.card-unread').forEach(card => {
                card.classList.remove('card-unread');
                card.classList.add('card-read');
                const title = card.querySelector('.card-title');
                if (title) title.classList.remove('fw-bold');
            });

            // Reset all toggle buttons
            document.querySelectorAll('.read-toggle').forEach(btn => {
                btn.dataset.isRead = 'true';
                btn.title = 'Mark unread';
                btn.innerHTML = '◉';
            });

            // Zero the badge
            setUnreadBadge(feedId, 0);

            showToast('Marked All Read', data.count + ' article(s) marked as read.', true);
        }
    })
    .catch(error => {
        console.error('Mark all read error:', error);
    });
}

function getCurrentFeedId() {
    const activeButton = document.querySelector('button.active[data-url]');
    if (activeButton) {
        const url = activeButton.getAttribute('data-url');
        const parts = url.split('/');
        return parts[parts.length - 1];
    }
    return null;
}

function updateUnreadBadge(feedId, delta) {
    const badge = document.querySelector('.unread-badge[data-feed-id="' + feedId + '"]');
    if (!badge) return;

    const current = parseInt(badge.textContent) || 0;
    const newCount = Math.max(0, current + delta);
    badge.textContent = newCount;
    badge.classList.toggle('d-none', newCount === 0);
}

function setUnreadBadge(feedId, count) {
    const badge = document.querySelector('.unread-badge[data-feed-id="' + feedId + '"]');
    if (!badge) return;

    badge.textContent = count;
    badge.classList.toggle('d-none', count === 0);
}

function bindReadToggles() {
    document.querySelectorAll('.read-toggle').forEach(button => {
        button.removeEventListener('click', readToggleClickHandler);
        button.addEventListener('click', readToggleClickHandler);
    });
}

function readToggleClickHandler() {
    const entryId = this.dataset.entryId;
    if (entryId) {
        toggleRead(entryId, this);
    }
}

function bindExternalReadButtons() {
    document.querySelectorAll('.external-read').forEach(button => {
        button.removeEventListener('click', externalReadClickHandler);
        button.addEventListener('click', externalReadClickHandler);
    });
}

function externalReadClickHandler() {
    handleExternalRead(this);
}

function bindMarkAllRead() {
    document.querySelectorAll('.mark-all-read').forEach(button => {
        button.removeEventListener('click', markAllReadClickHandler);
        button.addEventListener('click', markAllReadClickHandler);
    });
}

function markAllReadClickHandler() {
    const feedId = this.dataset.feedId;
    markAllRead(feedId);
}

document.addEventListener("DOMContentLoaded", function () {


    let tabButtons = bindFeeds();

    bindDeleteModalHandlers();
    bindFormHandlers();
    bindRefreshButtons();
    bindReadToggles();
    bindExternalReadButtons();
    bindMarkAllRead();

    // Handle back/forward
    window.addEventListener("popstate", function () {
        const tab = getCurrentTabFromURL();

        if (!tab) {
            // Clear active state from all buttons
            tabButtons.forEach(btn => btn.classList.remove("active"));

            // Check to see if we need an article
            const pathname = this.window.location.pathname

            if (pathname.toLowerCase().startsWith("/article")) {
                // Replace main content with default message
                getArticle();
                return;
            }

            // Replace main content with default message
            document.getElementById("main-content").innerHTML = `
            <div class="h-100 d-flex justify-content-center align-items-center">
                Select a tab to load content...
            </div>
        `;
            return;
        }
        tabButtons = bindFeeds();
        const button = [...tabButtons].find(btn =>
            btn.getAttribute("data-url").includes(tab)
        );

        console.log("Popstate\nTab: " + tab + "\nButton: " + button);
        if (button) {
            loadTab(button, tabButtons, false); // Don't push again during popstate
        }
    });

    // Initial page load
    const initialTab = getCurrentTabFromURL();
    if (initialTab) {
        const button = [...tabButtons].find(btn =>
            btn.getAttribute("data-url").includes(initialTab)
        );
        if (button) {
            loadTab(button, tabButtons, false); // No pushState during first load
        }
    }
});