return GetProductPrice();

function GetProductPrice() 
{
    if (HaveSalePrice())
    {
        return jQuery("p.price ins").text();
    }
    return jQuery("p.price").text();
}

function HaveSalePrice()
{
    return ReferenceEquals(jQuery("p.price").children().length, 2);
}