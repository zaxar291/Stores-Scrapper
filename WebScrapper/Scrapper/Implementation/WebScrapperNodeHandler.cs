using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using ScrapySharp.Extensions;
using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Services;

namespace WebApplication.Scrapper.Implementation
{
    public class WebScrapperNodeHandler : BaseNodeHandler<HtmlNode>
    {
        public string LastErrorMsg { get; set; }
        private IScrapperBaseValidator validationService { get; set; }
        public HtmlNode Node { get; private set; }

        public WebScrapperNodeHandler(HtmlNode HtmlNode)
        {
            Node = HtmlNode;
            validationService = new WebScrapperSelectorValidator();
        }

        #region Single node selectors
        
        public override HtmlNode SelectSingleElement(string selector)
        {
            if (ReferenceEquals(selector, null)) {
                return null;
            }
            if (selector.Contains(".") || selector.Contains("#") || !selector.Contains("/"))
            {
                return SingleSelectByCssSelector(selector);
            }
            return SingleSelectByXpath(selector);
        }

        public override HtmlNode SingleSelectByXpath(string selector)
        {
            if (validationService.ValidateWebSelector(selector))
            {
                try
                {
                    var selected = Node.SelectSingleNode(selector);
                    if (selected == null)
                    {
                        throw new Exception();
                    }

                    return selected;
                }    
                catch (Exception e)
                {
                    LastErrorMsg = $"{e.Message} -> {e.StackTrace}";
                    return null;
                }
            }
            else
            {
                LastErrorMsg =$"Selector {selector} is not a valid selector!";
                return null;
            }
        }

        public override HtmlNode SingleSelectByCssSelector(string selector)
        {
            if (validationService.ValidateWebSelector(selector))
            {
                try
                {
                    var selected = Node.CssSelect(selector);
                    if (selected == null)
                    {
                        throw new Exception();
                    }

                    var firstOrDefaultNode = selected.FirstOrDefault();
                    if (firstOrDefaultNode == null)
                    {
                        throw new Exception();
                    }

                    return firstOrDefaultNode;
                }
                catch (Exception e)
                {
                    LastErrorMsg = $"{e.Message} -> {e.StackTrace}";
                    return null;
                }
            }
            else
            {
                LastErrorMsg = $"Selector {selector} is not a valid selector!";
                return null;
            }
        }
        #endregion

        #region Multiple node selectors

        public override List<HtmlNode> SelectElementsCollection(string selector)
        {
            if (selector.Contains("#") || selector.Contains(".") || !selector.Contains("/"))
            {
                return SelectElementsCollectionByCssSelector(selector);
            }

            return SelectElementsCollectionByXPath(selector);
        }

        protected override List<HtmlNode> SelectElementsCollectionByCssSelector(string selector)
        {
            if (validationService.ValidateWebSelector(selector))
            {
                try
                {
                    var selectedCssNodeCollection = Node.CssSelect(selector);
                    if (selectedCssNodeCollection == null)
                    {
                        throw new NullReferenceException();
                    }
                    return selectedCssNodeCollection.ToList();
                }
                catch (NullReferenceException e)
                {
                    LastErrorMsg = $"(NullReferenceException) -> {e.Message}, stack trace: {e.StackTrace}";
                    return null;
                }
                catch (Exception e)
                {
                    LastErrorMsg = $"(Exception) -> {e.Message}, stack trace: {e.StackTrace}";
                    return null;
                }
            }
            else
            {
                LastErrorMsg = $"Selector {selector} is not a valid selector!";
                return null;
            }
        }

        protected override List<HtmlNode> SelectElementsCollectionByXPath(string selector)
        {
            if (validationService.ValidateWebSelector(selector))
            {
                try
                {
                    var xPathElementsNode = Node.SelectNodes(selector);
                    if (xPathElementsNode == null)
                    {
                        throw new NullReferenceException();
                    }

                    return xPathElementsNode.ToList();
                }
                catch (NullReferenceException e)
                {
                    LastErrorMsg = $"(NullReferenceException) -> {e.Message}, stack trace: {e.StackTrace}";
                    return null;
                } 
                catch (Exception e)
                {
                    LastErrorMsg = $"(Exception) -> {e.Message}, stack trace: {e.StackTrace}";
                    return null;
                }
            }
            else
            {
                LastErrorMsg = $"Selector {selector} is not a valid selector!";
                return null;
            }
        }

        #endregion
    }
}