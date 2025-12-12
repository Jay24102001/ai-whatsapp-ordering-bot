"use strict";

/* ============================================================
   CONSTANTS & LOCAL CART HELPERS
   ============================================================ */

var CART_KEY = "aibot_cart";

function getLocalCart() {
    try {
        return JSON.parse(localStorage.getItem(CART_KEY) || "[]");
    } catch (e) {
        return [];
    }
}

function saveLocalCart(c) {
    localStorage.setItem(CART_KEY, JSON.stringify(c));
}

function escapeHtml(str) {
    if (!str) return "";
    return str
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

function addonsSummary(addons) {
    return addons
        .map(function(a) {
            var qty = a.quantity > 1 ? " ×" + a.quantity : "";
            return escapeHtml(a.name) + qty;
        })
        .join(", ");
}

/* ============================================================
   MINI CART RENDER (LOCAL UI ONLY)
   ============================================================ */

function renderMiniCart() {
    var cart = getLocalCart();
    var cartCountBadge = document.getElementById("cartCount");

    if (cartCountBadge) cartCountBadge.innerText = cart.length;

    var el = document.getElementById("cart-placeholder");
    if (!el) return;

    if (cart.length === 0) {
        el.innerHTML = '<div class="text-muted">Cart is empty</div>';
        return;
    }

    var html = '<ul class="list-group">';
    cart.forEach(function(it) {
        var line = it.lineTotal || it.price * it.quantity;

        html +=
            '<li class="list-group-item d-flex justify-content-between align-items-center">' +
            '<div>' +
            "<strong>" + escapeHtml(it.name) + "</strong><br/>" +
            '<small class="text-muted">' + addonsSummary(it.addons || []) + "</small>" +
            "</div>" +
            '<div>' +
            it.quantity + " × ₹" + Number(it.price).toFixed(2) + "<br/>" +
            "<small>₹" + Number(line).toFixed(2) + "</small>" +
            "</div>" +
            "</li>";
    });
    html += "</ul>";
    el.innerHTML = html;
}

/* ============================================================
   SERVER CART BADGE UPDATE
   ============================================================ */

function updateCartCount(count) {
    var el = document.getElementById("cartCount");
    if (el) el.innerText = count;
}

/* ============================================================
   EVENT HANDLERS: QUICK ADD & OPEN OFFCANVAS
   ============================================================ */

document.addEventListener("click", async function(e) {
    var el = e.target;
    if (!el) return;

    /* QUICK ADD */
    if (el.dataset.action === "quick-add") {
        var id = parseInt(el.dataset.id);
        var name = el.dataset.name;
        var price = parseFloat(el.dataset.price);

        var cart = getLocalCart();
        cart.push({
            menuItemId: id,
            name: name,
            price: price,
            quantity: 1,
            addons: [],
            lineTotal: price
        });

        saveLocalCart(cart);
        renderMiniCart();

        try {
            var payload = { menuItemId: id, quantity: 1, addonIds: [] };
            var res = await fetch("/Orders/AddToCart", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                var json = await res.json();
                var serverCount = json.cartCount;
                updateCartCount(serverCount ? serverCount : cart.length);
            }
        } catch (err) {
            console.warn("Quick add error", err);
        }

        return;
    }

    /* OPEN OFFCANVAS */
    if (el.dataset.action === "open-offcanvas") {
        var id2 = parseInt(el.dataset.id);
        openOffcanvas(id2);
    }
});

/* ============================================================
   OPEN OFFCANVAS & FETCH ADDONS
   ============================================================ */

async function openOffcanvas(menuItemId) {
    var offcanvasEl = document.getElementById("addonOffcanvas");
    var offcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);

    document.getElementById("offcanvasMenuItemId").value = menuItemId;
    document.getElementById("qty").value = 1;
    document.getElementById("calculatedTotal").innerText = "0.00";
    document.getElementById("menuItemSummary").innerHTML = "";
    document.getElementById("addonsList").innerHTML = '<div class="text-muted">Loading...</div>';

    try {
        var resp = await fetch("/Orders/GetMenuItemWithAddons?menuItemId=" + menuItemId);

        if (!resp.ok) {
            document.getElementById("addonsList").innerHTML =
                '<div class="text-danger">Failed to load.</div>';
            offcanvas.show();
            return;
        }

        var dto = await resp.json();
        var item = dto.menuItem;
        var addons = dto.addons || [];

        offcanvasEl.dataset.basePrice = Number(item.price || 0);

        var imgHtml = item.imageUrl ?
            '<img src="' + escapeHtml(item.imageUrl) + '" style="height:60px;width:auto;margin-right:10px;" />' :
            "";

        document.getElementById("menuItemSummary").innerHTML =
            '<div class="d-flex align-items-center">' +
            imgHtml +
            '<div>' +
            '<div class="fw-bold">' + escapeHtml(item.name) + '</div>' +
            '<div class="text-muted">₹' + Number(item.price).toFixed(2) + "</div>" +
            "</div>" +
            "</div>";

        if (addons.length === 0) {
            document.getElementById("addonsList").innerHTML =
                '<div class="text-muted">No add-ons available.</div>';
        } else {
            var html = "";
            addons.forEach(function(a) {
                html +=
                    '<div class="form-check mb-2">' +
                    '<input class="form-check-input addon-checkbox" type="checkbox" value="' + a.id + '" data-price="' + Number(a.price) + '" id="addon_' + a.id + '">' +
                    '<label class="form-check-label" for="addon_' + a.id + '">' +
                    escapeHtml(a.name) + ' (+₹' + Number(a.price).toFixed(2) + ")" +
                    "</label>" +
                    "</div>";
            });
            document.getElementById("addonsList").innerHTML = html;
        }

        document.querySelectorAll(".addon-checkbox").forEach(function(cb) {
            cb.addEventListener("change", calculateTotal);
        });

        document.getElementById("qty").addEventListener("input", calculateTotal);

        calculateTotal();
        offcanvas.show();
    } catch (err) {
        console.error("Offcanvas error", err);
        document.getElementById("addonsList").innerHTML =
            '<div class="text-danger">Error loading data.</div>';
        offcanvas.show();
    }
}

/* ============================================================
   CALCULATE TOTAL
   ============================================================ */

function calculateTotal() {
    var offcanvasEl = document.getElementById("addonOffcanvas");
    var base = Number(offcanvasEl.dataset.basePrice || 0);
    var qty = Math.max(1, parseInt(document.getElementById("qty").value || "1"));

    var addonsTotal = 0;
    document.querySelectorAll(".addon-checkbox:checked").forEach(function(cb) {
        addonsTotal += Number(cb.dataset.price || 0);
    });

    var total = (base + addonsTotal) * qty;
    document.getElementById("calculatedTotal").innerText = total.toFixed(2);
}

/* ============================================================
   ADD TO CART (WITH ADDONS)
   ============================================================ */

document.addEventListener("DOMContentLoaded", function() {
    var btn = document.getElementById("addToCartBtn");
    if (!btn) return;

    btn.addEventListener("click", async function() {
        var menuItemId = parseInt(document.getElementById("offcanvasMenuItemId").value);
        var qty = Math.max(1, parseInt(document.getElementById("qty").value || "1"));

        var addonIds = Array.from(
            document.querySelectorAll(".addon-checkbox:checked")
        ).map(function(cb) {
            return parseInt(cb.value);
        });

        var nameEl = document.querySelector(
            ".menu-card[data-id='" + menuItemId + "'] .card-title"
        );
        var itemName = nameEl ? nameEl.innerText.trim() : "Item " + menuItemId;

        var basePrice = Number(
            document.getElementById("addonOffcanvas").dataset.basePrice || 0
        );

        var addons = Array.from(
            document.querySelectorAll(".addon-checkbox:checked")
        ).map(function(cb) {
            var lbl = cb.nextElementSibling ? cb.nextElementSibling.innerText : "";
            var clean = lbl.replace(/\(\+.*\)$/g, "").trim();
            return {
                id: parseInt(cb.value),
                name: clean,
                price: Number(cb.dataset.price),
                quantity: 1
            };
        });

        var lineTotal =
            (basePrice + addons.reduce(function(s, a) { return s + a.price; }, 0)) * qty;

        var localCart = getLocalCart();
        localCart.push({
            menuItemId: menuItemId,
            name: itemName,
            price: basePrice,
            quantity: qty,
            addons: addons,
            lineTotal: lineTotal
        });

        saveLocalCart(localCart);
        renderMiniCart();

        try {
            var resp = await fetch("/Orders/AddToCart", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ menuItemId: menuItemId, quantity: qty, addonIds: addonIds })
            });

            if (resp.ok) {
                var j = await resp.json();
                var serverCount = j.cartCount;
                updateCartCount(serverCount ? serverCount : localCart.length);
            }
        } catch (err) {
            console.warn("AddToCart error", err);
        }

        var off = bootstrap.Offcanvas.getInstance(
            document.getElementById("addonOffcanvas")
        );
        if (off) off.hide();
    });
});

/* ============================================================
   INITIAL LOAD: ONLY RUN GetCartCount ON MENU PAGE
   ============================================================ */

(async function init() {

    var path = window.location.pathname.toLowerCase();

    // ONLY menu page calls GetCartCount
    if (path.includes("/orders/menu") || path.includes("/menu")) {
        try {
            var resp = await fetch("/Orders/GetCartCount");
            if (resp.ok) {
                var j = await resp.json();
                updateCartCount(j.count || 0);
            }
        } catch (e) {}
    }

    renderMiniCart();
})();