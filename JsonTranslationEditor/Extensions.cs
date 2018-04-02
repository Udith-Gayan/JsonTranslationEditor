﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace JsonTranslationEditor
{
    public static class Extensions
    {

        // temporary until option is added...how much preloading of the tree is done
        private const int MaxDepth = 2;



        public static IEnumerable<LanguageSetting> ForParse(this IEnumerable<LanguageSetting> settings)
        {
            return settings.Where(o => !string.IsNullOrWhiteSpace(o.Namespace));
        }
        public static IEnumerable<LanguageSetting> NoEmpty(this IEnumerable<LanguageSetting> settings)
        {
            return settings.ForParse().Where(o => !string.IsNullOrWhiteSpace(o.Value));
        }
        public static IEnumerable<LanguageSetting> ExcludeLanguage(this IEnumerable<LanguageSetting> settings, string language)
        {
            return settings.Where(o => o.Language != language);
        }
        public static IEnumerable<LanguageSetting> OnlyLanguage(this IEnumerable<LanguageSetting> settings, string language)
        {
            return settings.Where(o => o.Language == language);
        }

        public static IEnumerable<string> ToNamespaces(this IEnumerable<LanguageSetting> settings)
        {
            return settings.ForParse().Select(o => o.Namespace).Distinct();
        }
        public static IEnumerable<string> ToNamespaces(this IEnumerable<LanguageSetting> settings, string language)
        {
            return settings.NoEmpty().Where(o => o.Language == language).Select(o => o.Namespace).Distinct();
        }

        public static IEnumerable<string> ToLanguages(this IEnumerable<LanguageSetting> settings)
        {
            return settings.Select(o => o.Language).Distinct();
        }

        public static Dictionary<string, IEnumerable<LanguageSetting>> ToLanguageDictionary(this IEnumerable<LanguageSetting> settings)
        {
            var seperatedByLanguage = settings.GroupBy(o => o.Language).Select(o => new { Language = o.Key, Settings = o.Select(p => p) });
            var dictionary = new Dictionary<string, IEnumerable<LanguageSetting>>();

            foreach (var matches in seperatedByLanguage)
            {
                dictionary.Add(matches.Language, matches.Settings.ForParse());
            }
            return dictionary;
        }

        public static IEnumerable<NsTreeItem> ToNsTree(this IEnumerable<LanguageSetting> settings)
        {
            var namespaces = settings.Select(o => o.Namespace.Split('.')[0]).Distinct().OrderBy(o => o).ToList();
            var root = new NsTreeItem() { Name = "root" };

            foreach (var ns in namespaces)
            {
                ProcessNs(root, ns, settings.ToList());
            }
            
            var nodes = new List<NsTreeItem>();
            foreach (NsTreeItem node in root.Items)
            {
                nodes.Add(node);
                node.Parent = null;
            }
            root.Clear();


            return nodes;
        }

    
        public static void ProcessNs(NsTreeItem node, string ns, List<LanguageSetting> allSettings, int depth = 1, int customDepth = 0)
        {
            if (customDepth == 0)
                customDepth = MaxDepth;

            var thisNode = new NsTreeItem() { Parent = node, Name = (ns.Split('.').Last()), Namespace = ns , ImagePath = "Assets/Images/ns.png" };

            if (node == null)
                node = thisNode;
            else
            {
                node.AddChild(thisNode);
            }

            var namespaces = allSettings.Where(o => o.Namespace.StartsWith(ns + ".")).Select(o => o.Namespace.Substring(ns.Length + 1).Split('.')[0]).Distinct().OrderBy(o => o).ToList();

            if (!namespaces.Any())
            {
                thisNode.ImagePath = "Assets/Images/translation.png";
                thisNode.Settings = allSettings.Where(o => o.Namespace == thisNode.Namespace);
                return;
            }

            var applicableSettings = allSettings.Where(o => o.Namespace.StartsWith(ns + ".")).ToList();


            if (depth > customDepth)
            {
                thisNode.HeldSetttings = applicableSettings;
                return;
            }


            depth++;
            foreach (var nextNs in namespaces)
            {

                ProcessNs(thisNode, $"{ns}.{nextNs}", applicableSettings, depth);
            }

            thisNode.IsLoaded = true;
        }


        public static void MergeValues(this Dictionary<string,string> list1,Dictionary<string,string> list2)
        {
            foreach (var item in list1.Keys)
            {
                if (list2.ContainsKey(item))
                {
                    list1[item] += list2[item];
                }
            }
      
            
        }

        public static IEnumerable<TreeViewItem> ToTreeView(this IEnumerable<LanguageSetting> settings)
        {
            var namespaces = settings.Select(o => o.Namespace.Split('.')[0]).Distinct().OrderBy(o => o).ToList();
            var root = new TreeViewItem() { Header = "root", Tag = "Assets/Images/ns.png" };

            foreach (var ns in namespaces)
            {
                ProcessNs(root, ns, settings);
            }

            var nodes = new List<TreeViewItem>();
            foreach (TreeViewItem node in root.Items)
            {
                nodes.Add(node);
            }
            root.Items.Clear();
            return nodes;
        }

        private static void ProcessNs(TreeViewItem node, string ns, IEnumerable<LanguageSetting> allSettings)
        {
            var thisNode = new TreeViewItem() { Header = (ns.Split('.').Last()), Tag = node.Tag };

            if (node == null)
                node = thisNode;
            else
                node.Items.Add(thisNode);

            var namespaces = allSettings.Where(o => o.Namespace.StartsWith(ns + ".")).Select(o => o.Namespace.Substring(ns.Length + 1).Split('.')[0]).Distinct().OrderBy(o => o).ToList();

            if (!namespaces.Any())
            {
                thisNode.Tag = "Assets/Images/translation.png";
                return;
            }

            foreach (var nextNs in namespaces)
            {
                ProcessNs(thisNode, ns + "." + nextNs, allSettings);
            }

        }

        public static string ToNamespaceString(this TreeViewItem node, string built = "")
        {
            if (node == null) return built;

            if (node.Parent == null && string.IsNullOrEmpty(built))
                return node.Header.ToString();


            if (node.Parent is TreeViewItem)
            {
                return ToNamespaceString((TreeViewItem)node.Parent, built = string.IsNullOrEmpty(built) ? node.Header.ToString() : node.Header.ToString() + "." + built);
            }
            else
            {
                return (node.Header + "." + built);
            }

        }

        public static bool SelectByNamespace(this ItemCollection nodes, string ns)
        {
            if (nodes.Count == 0) return false;
            if (string.IsNullOrEmpty(ns)) return false;

            foreach (TreeViewItem node in nodes)
            {
                var foundNode = WalkToNamespace(node, ns);
                if (foundNode != null)
                {
                    foundNode.IsSelected = true;
                    return true;
                }
            }

            return false;
        }

        private static TreeViewItem WalkToNamespace(TreeViewItem node, string remainingNs)
        {
            if (node == null)
                return null;

            if (node.Header.ToString() == remainingNs)
                return node;

            if (!node.HasItems)
                return null;

            var parts = remainingNs.Split('.');
            var nsPart = parts[0];

            if (node.Header.ToString() != nsPart)
                return null;

            node.IsExpanded = true;

            var nsRemaining = remainingNs.Substring(nsPart.Length + 1);
            foreach (TreeViewItem child in node.Items)
            {
                var childResult = WalkToNamespace(child, nsRemaining);
                if (childResult != null)
                    return childResult;
            }

            return null;
        }


    }
}
