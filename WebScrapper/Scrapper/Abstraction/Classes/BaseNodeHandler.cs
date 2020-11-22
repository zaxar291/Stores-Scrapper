using System.Collections.Generic;
namespace WebApplication.Scrapper.Abstraction
{
    public abstract class BaseNodeHandler<TBrowserNode>
    {
        
        public TBrowserNode Node { get; private set; }

        #region Single node selectors
        
        public virtual TBrowserNode SelectSingleElement(string selector)
        {
            if (selector.Contains("#") || selector.Contains(".") || selector.Equals("script"))
            {
                return SingleSelectByCssSelector(selector);
            }

            return SingleSelectByXpath(selector);
        }

        public virtual TBrowserNode SingleSelectByCssSelector(string selector)
        {
            return Node;
        }

        public virtual TBrowserNode SingleSelectByXpath(string selector)
        {
            return Node;
        }
        
        #endregion

        #region Multiple node selectors
        public virtual List<TBrowserNode>SelectElementsCollection(string selector)
        {
            if (selector.Contains("#") || selector.Contains("."))
            {
                return SelectElementsCollectionByCssSelector(selector);
            }

            return SelectElementsCollectionByXPath(selector);
        }

        protected virtual List<TBrowserNode> SelectElementsCollectionByCssSelector(string selector)
        {
            return new List<TBrowserNode>();
        }

        protected virtual List<TBrowserNode> SelectElementsCollectionByXPath(string selector)
        {
            return new List<TBrowserNode>();
        }
        #endregion
    }
}