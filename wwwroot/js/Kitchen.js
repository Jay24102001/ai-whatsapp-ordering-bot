"use strict";

(async function() {

    // --------------------------------------------------------
    // SIGNALR
    // --------------------------------------------------------
    var connection = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .withAutomaticReconnect()
        .build();

    connection.on("NewOrder", function(order) {
        console.log("SignalR NewOrder:", order);
        safeAddOrUpdate(order);
    });

    connection.on("OrderUpdated", function(order) {
        console.log("SignalR OrderUpdated:", order);
        safeAddOrUpdate(order);
    });

    connection.onclose(function(error) {
        console.warn("SignalR connection closed", error);
    });

    try {
        await connection.start();
        console.log("Kitchen SignalR Connected");
    } catch (err) {
        console.error("Failed to start SignalR connection:", err);
    }

    // --------------------------------------------------------
    // STATUS VALUES (for reference)
    // 1 = Received
    // 2 = InKitchen
    // 3 = Preparing
    // 4 = Ready
    // 5 = Completed
    // --------------------------------------------------------

    // --------------------------------------------------------
    // HELPERS
    // --------------------------------------------------------
    function findCard(id) {
        return document.querySelector('.order-card[data-order-id="' + id + '"]');
    }

    function removeCard(id) {
        var c = findCard(id);
        if (c) c.parentNode.removeChild(c);
    }

    function safeAddOrUpdate(order) {
        try {
            if (!order) {
                console.warn("Received null/undefined order");
                return;
            }
            var id = order.Id || order.id || order.orderId;
            if (!id) {
                console.warn("Order missing Id:", order);
                return;
            }
            addOrUpdate(order);
        } catch (err) {
            console.error("safeAddOrUpdate error:", err, order);
        }
    }

    // --------------------------------------------------------
    // CARD HTML BUILDER
    // --------------------------------------------------------
    function buildCard(order) {

        var orderItems = [];
        if (Array.isArray(order.OrderItems)) orderItems = order.OrderItems;
        else if (Array.isArray(order.orderItems)) orderItems = order.orderItems;

        var itemsHtml = "";
        for (var i = 0; i < orderItems.length; i++) {
            var it = orderItems[i];
            var menuName = "Item";
            if (it && it.MenuItem && it.MenuItem.Name) menuName = it.MenuItem.Name;
            else if (it && it.MenuItem && it.MenuItem.name) menuName = it.MenuItem.name;

            var qty = 1;
            if (it && typeof it.Quantity !== "undefined") qty = it.Quantity;
            else if (it && typeof it.quantity !== "undefined") qty = it.quantity;

            var lineTotal = 0;
            if (it && typeof it.LineTotal !== "undefined") lineTotal = it.LineTotal;
            else if (it && typeof it.lineTotal !== "undefined") lineTotal = it.lineTotal;

            itemsHtml += '<div class="line-item">' +
                '<div><strong>' + escapeHtml(menuName) + '</strong> x ' + qty + '</div>' +
                '<div class="text-muted small">' + Number(lineTotal).toFixed(2) + '</div>' +
                '</div>';
        }

        var totalAmount = 0;
        if (typeof order.TotalAmount !== "undefined") totalAmount = order.TotalAmount;
        else if (typeof order.totalAmount !== "undefined") totalAmount = order.totalAmount;

        var createdAt = order.CreatedAt || order.createdAt || "";
        var time = "";
        if (createdAt) {
            try {
                time = new Date(createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
            } catch (e) {
                time = "";
            }
        }

        var customerName = order.CustomerName || order.customerName || "Walk-in";
        var id = order.Id || order.id || order.orderId;

        var html = '' +
            '<div class="order-card" data-order-id="' + id + '">' +
            '  <div class="order-header">' +
            '    <strong>Order #' + id + '</strong>' +
            '    <small class="text-muted">' + escapeHtml(customerName) + ' • ' + escapeHtml(time) + '</small>' +
            '  </div>' +
            '  <div class="order-body">' + itemsHtml + '</div>' +
            '  <div class="order-footer d-flex justify-content-between align-items-center">' +
            '    <div><strong>Total:</strong> ' + Number(totalAmount).toFixed(2) + '</div>' +
            '    <div class="btn-group">' +
            '      <button class="btn btn-sm btn-success move-next" data-id="' + id + '">Move Next</button>' +
            '      <button class="btn btn-sm btn-outline-secondary move-back" data-id="' + id + '">Move Back</button>' +
            '    </div>' +
            '  </div>' +
            '</div>';

        return html;
    }

    // Small HTML escape for safety
    function escapeHtml(str) {
        if (typeof str !== "string") return str;
        return str.replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    // --------------------------------------------------------
    // ADD / UPDATE ORDER CARD
    // --------------------------------------------------------
    function addOrUpdate(order) {
        var id = order.Id || order.id || order.orderId;
        var status = Number(order.Status || order.status || 0);

        if (!id) {
            console.warn("addOrUpdate: no id", order);
            return;
        }

        var column = "col-in-kitchen";
        if (status === 3) column = "col-preparing";
        else if (status === 4) column = "col-ready";

        if (status >= 5) {
            removeCard(id);
            return;
        }

        removeCard(id);

        var container = document.getElementById(column);
        if (!container) {
            console.warn("addOrUpdate: container not found:", column);
            return;
        }

        container.insertAdjacentHTML("afterbegin", buildCard(order));

        var card = findCard(id);
        if (card) {
            card.classList.add("card-pop");
            setTimeout(function() {
                card.classList.remove("card-pop");
            }, 900);
        }

        bindButtons(String(id));
    }

    // --------------------------------------------------------
    // BIND MOVE NEXT / MOVE BACK BUTTONS
    // --------------------------------------------------------
    function bindButtons(id) {
        var card = findCard(id);
        if (!card) return;

        var next = card.querySelector(".move-next");
        var back = card.querySelector(".move-back");

        function setButtonsDisabled(disabled) {
            if (next) next.disabled = disabled;
            if (back) back.disabled = disabled;
        }

        if (next) {
            next.onclick = function() {
                if (card.closest && card.closest("#col-in-kitchen")) {
                    updateStatus(id, 3);
                } else if (card.closest && card.closest("#col-preparing")) {
                    updateStatus(id, 4);
                } else if (card.closest && card.closest("#col-ready")) {
                    updateStatus(id, 5);
                }
            };
        }

        if (back) {
            back.onclick = function() {
                if (card.closest && card.closest("#col-ready")) {
                    updateStatus(id, 3);
                } else if (card.closest && card.closest("#col-preparing")) {
                    updateStatus(id, 2);
                } else {
                    updateStatus(id, 2);
                }
            };
        }

        // internal wrapper to toggle disabled during network call
        function updateStatus(idVal, statusVal) {
            setButtonsDisabled(true);
            _updateStatusRequest(idVal, statusVal).finally(function() {
                // brief re-enable; server push will re-render card with authoritative state
                setTimeout(function() { setButtonsDisabled(false); }, 400);
            });
        }
    }

    // Bind any existing cards on page load
    var existing = document.querySelectorAll(".order-card");
    for (var i = 0; i < existing.length; i++) {
        try {
            bindButtons(existing[i].dataset.orderId);
        } catch (e) {
            // ignore
        }
    }

    // --------------------------------------------------------
    // NETWORK CALL
    // --------------------------------------------------------
    function _updateStatusRequest(id, status) {
        var orderIdNum = Number(id);
        var statusNum = Number(status);

        if (!orderIdNum || !statusNum) {
            alert("Invalid order or status");
            return Promise.resolve();
        }

        return fetch("/Orders/UpdateStatus", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
                    // Add anti-forgery header here if you enable it server-side
            },
            credentials: "same-origin",
            body: JSON.stringify({
                orderId: orderIdNum,
                status: statusNum
            })
        }).then(function(res) {
            if (!res.ok) {
                return res.text().then(function(text) {
                    console.error("UpdateStatus failed:", res.status, text);
                    alert("Failed to update order status");
                }).catch(function() {
                    console.error("UpdateStatus failed with status:", res.status);
                    alert("Failed to update order status");
                });
            } else {
                console.log("UpdateStatus request OK for order " + orderIdNum + " → " + statusNum);
            }
        }).catch(function(err) {
            console.error("Network error while updating status:", err);
            alert("Network error while updating order status");
        });
    }

})();