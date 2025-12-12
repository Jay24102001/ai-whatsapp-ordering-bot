// wwwroot/js/order.js

let cart = []; // { menuItemId, name, unitPrice, qty, addons: [{addonId, name, qty, unitPrice}] }
let currentModalItem = { id: 0, name: '', price: 0 };

function openItemModal(menuItemId, name, price) {
    currentModalItem = { id: menuItemId, name, price: parseFloat(price) };
    document.getElementById('modalTitle').innerText = `${name} - ₹${price.toFixed(2)}`;
    document.getElementById('modalItemQty').value = 1;
    loadAddonsForItem(menuItemId);
    updateLinePreview();
    new bootstrap.Modal(document.getElementById('itemModal')).show();
}

function changeItemQty(delta) {
    let el = document.getElementById('modalItemQty');
    let v = parseInt(el.value || '1') + delta;
    if (v < 1) v = 1;
    el.value = v;
    updateLinePreview();
}

async function loadAddonsForItem(menuItemId) {
    // fetch add-ons that can be attached to this menu item.
    // For simplicity, endpoint: /MenuItems/GetAddonsForItem/{id}
    // We'll implement a small controller action or reuse an existing endpoint. If not present, create /api/addons?menuItemId=X
    const res = await fetch(`/MenuItems/GetAddonsForItem/${menuItemId}`);
    const list = res.ok ? await res.json() : [];
    const parent = document.getElementById('modalAddonsList');
    parent.innerHTML = '';
    list.forEach(a => {
        const row = document.createElement('div');
        row.className = 'd-flex align-items-center mb-2';
        row.innerHTML = `
            <div class="flex-grow-1">
                <strong>${a.name}</strong> <small>₹${parseFloat(a.price).toFixed(2)}</small>
            </div>
            <div style="width:120px;">
                <div class="input-group input-group-sm">
                    <button class="btn btn-outline-secondary" type="button" onclick="changeAddonQty(${a.id}, -1)">-</button>
                    <input id="addonQty_${a.id}" class="form-control text-center" value="0" />
                    <button class="btn btn-outline-secondary" type="button" onclick="changeAddonQty(${a.id}, 1)">+</button>
                </div>
            </div>
        `;
        // store price on element dataset
        row.dataset.addonId = a.id;
        row.dataset.addonPrice = a.price;
        parent.appendChild(row);
    });
    updateLinePreview();
}

function changeAddonQty(addonId, delta) {
    const el = document.getElementById(`addonQty_${addonId}`);
    if (!el) return;
    let v = parseInt(el.value || '0') + delta;
    if (v < 0) v = 0;
    el.value = v;
    updateLinePreview();
}

function updateLinePreview() {
    const qty = parseInt(document.getElementById('modalItemQty').value || '1');
    let total = currentModalItem.price * qty;
    const addonEls = document.querySelectorAll('#modalAddonsList > div');
    addonEls.forEach(r => {
        const id = r.dataset.addonId;
        const price = parseFloat(r.dataset.addonPrice || '0');
        const qEl = document.getElementById(`addonQty_${id}`);
        const q = qEl ? parseInt(qEl.value || '0') : 0;
        total += price * q;
    });
    document.getElementById('linePreview').innerText = total.toFixed(2);
}

function addToCartFromModal() {
    const qty = parseInt(document.getElementById('modalItemQty').value || '1');
    const addonEls = document.querySelectorAll('#modalAddonsList > div');

    const addons = [];
    addonEls.forEach(r => {
        const id = parseInt(r.dataset.addonId);
        const price = parseFloat(r.dataset.addonPrice || '0');
        const qEl = document.getElementById(`addonQty_${id}`);
        const q = qEl ? parseInt(qEl.value || '0') : 0;
        if (q > 0) {
            const nameElement = r.querySelector('strong');
            const addonName = nameElement ? nameElement.innerText.trim() : '';

            addons.push({
                addonId: id,
                quantity: q,
                unitPrice: price,
                name: addonName
            });
        }
        if (q > 0) {
            const nameElement = r.querySelector('strong');
            const addonName = nameElement ? nameElement.innerText.trim() : '';

            addons.push({
                addonId: id,
                quantity: q,
                unitPrice: price,
                name: addonName
            });
        }
    });

    // check if same item with same addon composition exists; if so merge
    cart.push({
        menuItemId: currentModalItem.id,
        name: currentModalItem.name,
        unitPrice: currentModalItem.price,
        quantity: qty,
        addons: addons
    });

    updateCartUI();
    bootstrap.Modal.getInstance(document.getElementById('itemModal')).hide();
}

function updateCartUI() {
    const panel = document.getElementById('cartPanel');
    panel.innerHTML = '';
    if (!cart.length) {
        panel.innerHTML = '<p class="text-muted">No items</p>';
        return;
    }

    let html = '<ul class="list-group">';
    let grandTotal = 0;
    cart.forEach((ci, idx) => {
        let lineTotal = ci.unitPrice * ci.quantity;
        let addonsHtml = '';
        if (ci.addons && ci.addons.length) {
            addonsHtml = '<ul class="small">';
            ci.addons.forEach(a => {
                lineTotal += a.unitPrice * a.quantity;
                addonsHtml += `<li>${a.name} × ${a.quantity} (₹${(a.unitPrice*a.quantity).toFixed(2)})</li>`;
            });
            addonsHtml += '</ul>';
        }
        grandTotal += lineTotal;
        html += `<li class="list-group-item">
            <div class="d-flex justify-content-between">
                <div>
                    <strong>${ci.name}</strong> × ${ci.quantity} <br />
                    ${addonsHtml}
                </div>
                <div class="text-end">
                    ₹${lineTotal.toFixed(2)} <br/>
                    <button class="btn btn-sm btn-danger mt-1" onclick="removeCartItem(${idx})">Remove</button>
                </div>
            </div>
        </li>`;
    });
    html += `</ul><div class="mt-2"><strong>Total: ₹${grandTotal.toFixed(2)}</strong></div>`;
    panel.innerHTML = html;
}

function removeCartItem(index) {
    cart.splice(index, 1);
    updateCartUI();
}

async function placeOrder() {
    if (!cart.length) { alert('Cart is empty'); return; }
    const payload = {
        customerName: document.getElementById('customerName').value || null,
        orderType: parseInt(document.getElementById('orderType').value || '1'),
        items: cart.map(ci => ({
            menuItemId: ci.menuItemId,
            quantity: ci.quantity,
            unitPrice: ci.unitPrice,
            addons: (ci.addons || []).map(a => ({ addonId: a.addonId, quantity: a.quantity, unitPrice: a.unitPrice }))
        }))
    };

    // POST to Orders/Create
    const res = await fetch('/Orders/Create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (!res.ok) {
        const txt = await res.text();
        alert('Order failed: ' + txt);
        return;
    }

    const data = await res.json();
    alert('Order placed: ' + data.id);
    cart = [];
    updateCartUI();
}