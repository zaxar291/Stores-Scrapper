return GetProductOldPrice();

function GetProductOldPrice() 
{
    if (HaveSalePrice())
    {
        return jQuery("p.price del").text();
    }
    return 0;
}

function HaveSalePrice()
{
    return ReferenceEquals(jQuery("p.price").children().length, 2);
}