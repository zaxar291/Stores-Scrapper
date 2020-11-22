
return TaskGetCbdCigarettesPrice();

function TaskGetCbdCigarettesPrice()
{
    $ = jQuery;
    var priceNode = $(".price.product-page-price");
    if (priceNode.length > 0)
    {
        return TaskGetLowestPrice(priceNode);
    }
}

function TaskGetLowestPrice(node) 
{
    if (TaskHasSalePrice(node))
    {
        return $(node).find("ins .woocommerce-Price-amount.amount").text();
    }
    else
    {
        if (TaskIsRange(node))
        {
            return $($(node).find(".woocommerce-Price-amount")[0]).text();
        }
        return $(node).find(".woocommerce-Price-amount.amount").text();
    }
}

function TaskHasSalePrice(node)
{
    if (node.hasClass("price-on-sale"))
    {
        return true;
    }
    return false;
}

function TaskIsRange(node)
{
    if ($(node).text().indexOf(" – ") > -1)
    {
        return true;
    }
    return false;
}
//function TaskGetCbdCigarettesPrice(){$=jQuery;var e=$(".price.product-page-price");if(e.length>0)return TaskGetLowestPrice(e)}function TaskGetLowestPrice(e){return TaskHasSalePrice(e)?$(e).find("ins .woocommerce-Price-amount.amount").text():TaskIsRange(e)?$($(e).find(".woocommerce-Price-amount")[0]).text():$(e).find(".woocommerce-Price-amount.amount").text()}function TaskHasSalePrice(e){return!!e.hasClass("price-on-sale")}function TaskIsRange(e){return $(e).text().indexOf(" – ")>-1}