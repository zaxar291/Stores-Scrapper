return TaskGetCbdLuxePrice();

function TaskGetCbdLuxePrice() {
    $ = jQuery;
    var priceNode = $(".custom_rate3");
    if (priceNode.length > 0) {
        if (TaskIsRange(priceNode)) {
            return $($(priceNode).find(".woocommerce-Price-amount")[0]).text();
        }
        return $(priceNode).text();
    }
}

function TaskIsRange(node) {
    if ($(node).text().indexOf("–") > -1) {
        return true;
    }
    return false;
}