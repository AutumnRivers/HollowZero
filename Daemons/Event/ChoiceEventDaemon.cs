using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

using Hacknet;
using Hacknet.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;
using Pathfinder.Util;

using BepInEx;

using static HollowZero.HollowLogger;

namespace HollowZero.Daemons.Event
{
    public class ChoiceEventDaemon : EventDaemon
    {
        public ChoiceEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public static new bool Registerable => true;

        [XMLStorage]
        public string CEventTitle = "Choice Event";

        [XMLStorage(IsContent = true)]
        public string CEventContent = "Choice Event Content";

        public ChoiceEvent choiceEvent;
        public const int BUTTON_MARGIN = 10;
        public const string DEFAULT_EVENTS_FILE_PATH = HollowZeroCore.DEFAULT_CONFIG_PATH + "/Events/ChoiceEvents.xml";

        private static List<ChoiceEvent> possibleEvents = new List<ChoiceEvent>();
        internal static List<ChoiceEvent> PossibleEvents
        {
            get
            {
                return possibleEvents;
            }
            private set
            {
                possibleEvents = value;
            }
        }

        public override void initFiles()
        {
            base.initFiles();

            choiceEvent = PossibleEvents.GetRandom();
            ContentPages = choiceEvent.ContentPages;

            EventTitle = choiceEvent.Title;
            EventContent = choiceEvent.Content;

            comp.name = "??? UNKNOWN ???";
        }

        public override void navigatedTo()
        {
            base.navigatedTo();

            comp.name = choiceEvent.Title;

            if(choiceEvent.Unavoidable)
            {
                UnavoidableEventDaemon.LockUpModules();
            }
        }

        public bool tookAction = false;
        public int ContinueButtonID = PFButton.GetNextID();

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            DrawEventTemplate(bounds);

            int buttonOffset = 75;
            if(tookAction)
            {
                var b = new HollowButton(ContinueButtonID, bounds.X + 25,
                    bounds.Y + bounds.Height - buttonOffset, bounds.Width - 50, 50, "Continue...",
                    OS.currentInstance.defaultHighlightColor);
                b.OnPressed = delegate () { RemoveDaemon(); };
                b.DoButton();

                return;
            }

            for (var i = choiceEvent.Choices.Count - 1; i > -1; i--)
            {
                var choice = choiceEvent.Choices[i];
                Color buttonColor = choice.Color;
                var b = new HollowButton(choice.ButtonID, bounds.X + 25,
                    bounds.Y + bounds.Height - buttonOffset, bounds.Width - 50, 50, $"{choice.Title}\n{choice.Subtext}", buttonColor);

                if(choice.ChoiceType == "takecreds" && HollowZeroCore.PlayerCredits - choice.ChoiceAmount < 0)
                {
                    b.Disabled = true;
                    b.DisabledMessage = "<!> You don't have enough credits!";
                }

                foreach(var chance in choice.Chances)
                {
                    if(chance.Value.ChoiceType == "takecreds" && HollowZeroCore.PlayerCredits - chance.Value.ChoiceAmount < 0)
                    {
                        b.Disabled = true;
                        b.DisabledMessage = "<!> You don't have enough credits to take that risk!";
                    }
                }

                b.OnPressed = delegate ()
                {
                    if (choice.OnPressed != null)
                    {
                        if(choice.Page > 0)
                        {
                            EventContent = ContentPages[choice.Page];
                        }
                        choice.OnPressed.Invoke();
                    }
                    else
                    {
                        int luck = choice.TotalLuckValue;
                        Random random = new Random();
                        int luckValue = random.Next(0, luck);

                        foreach (var chance in choice.Chances)
                        {
                            if (luckValue < chance.Key)
                            {
                                if(chance.Value.Page > 0)
                                {
                                    EventContent = ContentPages[chance.Value.Page];
                                }
                                chance.Value.Trigger.Invoke();
                                break;
                            }
                        }
                    }

                    if(choiceEvent.Unavoidable)
                    {
                        UnavoidableEventDaemon.UnlockModules();
                    }

                    tookAction = true;
                };
                b.DoButton();
                buttonOffset += 60;
            }
        }

        public const string XML_TAG = "HollowZeroChoiceEvents";

        public static void ReadChoiceEventsFileRewrite(string filename = "")
        {
            filename = filename == "" ? ExtensionLoader.ActiveExtensionInfo.FolderPath + DEFAULT_EVENTS_FILE_PATH : filename;
            if (!File.Exists(filename)) { return; }

            LogDebug(HollowZeroCore.HZLOG_PREFIX + "[Rewrite] Reading choice events...");

            FileStream eventsFileStream = File.OpenRead(filename);
            XmlReader xml = XmlReader.Create(eventsFileStream);
            XDocument choiceDocument = XDocument.Load(xml);

            var choiceEvents = choiceDocument.Root.Elements("ChoiceEvent");

            foreach(var ev in choiceEvents)
            {
                ChoiceEvent cev = new ChoiceEvent()
                {
                    Title = ev.ReadRequiredAttribute("Title")
                };
                List<XAttribute> attributes = ev.Attributes().ToList();
                List<XElement> children = ev.Elements().ToList();

                if(attributes.TryFind(a => a.Name == "Unavoidable", out var unav))
                {
                    cev.Unavoidable = bool.Parse(unav.Value);
                }

                if(children.TryFind(c => c.Name == "FirstContact", out var firstContactElem))
                {
                    cev.FirstContactContent = firstContactElem.Value;
                }

                var content = ev.GetRequiredChild("Content");
                if(content.IsEmpty) { throw new MissingContentException(content); }
                cev.Content = content.Value;
                
                if(!cev.ContentPages.Any())
                {
                    cev.ContentPages.Add(cev.Content);
                }

                if(!children.Any(c => c.Name == "Choice")) { throw new FormatException("Missing choices in choice event " + cev.Title); }

                foreach(var choice in children.Where(c => c.Name == "Choice"))
                {
                    cev.Choices.Add(ReadChoiceDocument(choice, ref cev));
                }

                LogDebug(HollowZeroCore.HZLOG_PREFIX + "" +
                    $"[Rewrite] Registering choice event {cev.Title}...");
                PossibleEvents.Add(cev);
            }

            eventsFileStream.Close();
            xml.Close();

            Choice ReadChoiceDocument(XElement choiceElement, ref ChoiceEvent choiceEvent)
            {
                int currentPage = 1;

                Choice choice = new Choice()
                {
                    Title = choiceElement.ReadRequiredAttribute("Title"),
                    Subtext = choiceElement.ReadRequiredAttribute("Subtext"),
                    Color = ColorUtils.FromString(choiceElement.ReadRequiredAttribute("Color")),
                    ParentEvent = choiceEvent,
                    ChoiceAmount = -1,
                    ChoiceItem = ""
                };
                bool isChance = choiceElement.Elements().Any(c => c.Name == "ChoiceChance");

                if(!isChance)
                {
                    choice.ChoiceType = choiceElement.ReadRequiredAttribute("Type");
                    if(choiceElement.Attributes().ToList().TryFind(a => a.Name == "Amount", out var amAttr))
                    {
                        choice.ChoiceAmount = int.Parse(amAttr.Value);
                    }
                    if(choiceElement.Attributes().ToList().TryFind(a => a.Name == "ItemID", out var itemAttr))
                    {
                        choice.ChoiceItem = itemAttr.Value;
                    }
                    if(!choiceElement.IsEmpty)
                    {
                        choiceEvent.ContentPages.Add(choiceElement.Value);
                        choice.Page = choiceEvent.ContentPages.Count - 1;
                        currentPage++;
                    }
                    choice.OnPressed = delegate ()
                    {
                        DetermineActionFromType(choice.ChoiceType, choice.ChoiceAmount, choice.ChoiceItem).Invoke();
                    };
                } else
                {
                    foreach(var chance in choiceElement.Elements().Where(c => c.Name == "ChoiceChance"))
                    {
                        ChoiceChance choiceChance = new ChoiceChance()
                        {
                            ChoiceType = chance.ReadRequiredAttribute("Type"),
                            ChoiceAmount = -1,
                            ChoiceItem = ""
                        };
                        List<XAttribute> attributes = chance.Attributes().ToList();
                        
                        if(attributes.TryFind(a => a.Name == "Amount", out var amAttr))
                        {
                            choiceChance.ChoiceAmount = int.Parse(amAttr.Value);
                        }
                        if(attributes.TryFind(a => a.Name == "ItemID", out var itemAttr))
                        {
                            choiceChance.ChoiceItem = itemAttr.Value;
                        }
                        if(!chance.IsEmpty)
                        {
                            choiceEvent.ContentPages.Add(chance.Value);
                            choiceChance.Page = choiceEvent.ContentPages.Count - 1;
                            currentPage++;
                        }

                        choiceChance.Trigger = delegate ()
                        {
                            DetermineActionFromType(choiceChance.ChoiceType, choiceChance.ChoiceAmount, choiceChance.ChoiceItem).Invoke();
                        };
                        int luck = int.Parse(chance.ReadRequiredAttribute("Chance"));

                        choice.Chances.Add(luck, choiceChance);
                    }
                }

                return choice;
            }

            Action DetermineActionFromType(string type, int amount = 0, string item = "", int currentContentPage = -1)
            {
                type = type.ToLower();
                switch (type)
                {
                    case "upinf":
                        return delegate () { HollowZeroCore.IncreaseInfection(amount); };
                    case "downinf":
                        return delegate () { HollowZeroCore.DecreaseInfection(amount); };
                    case "clearinf":
                        return delegate () { HollowZeroCore.ClearInfection(); };
                    case "addmod":
                        return delegate ()
                        {
                            if(!item.IsNullOrWhiteSpace() && HollowZeroCore.PossibleModifications.TryFind(m => m.ID == item, out var mod))
                            {
                                HollowManager.AddModification(mod);
                            } else
                            {
                                HollowZeroCore.AddModification();
                            }
                        };
                    case "addcorrupt":
                        return delegate ()
                        {
                            if (!item.IsNullOrWhiteSpace() && HollowZeroCore.PossibleCorruptions.TryFind(m => m.ID == item, out var cor))
                            {
                                HollowManager.AddCorruption(cor);
                            }
                            else
                            {
                                HollowZeroCore.AddCorruption();
                            }
                        };
                    case "addcreds": return delegate () { HollowZeroCore.AddPlayerCredits(amount); };
                    case "takecreds": return delegate () { HollowZeroCore.RemovePlayerCredits(amount); };
                    case "addmalware": return delegate ()
                        {
                            if (!item.IsNullOrWhiteSpace() && HollowZeroCore.PossibleMalware.TryFind(m => m.DisplayName == item, out var malware))
                            {
                                HollowManager.AddMalware(malware);
                            }
                            else
                            {
                                HollowZeroCore.AddMalware();
                            }
                        };
                    case "none":
                    default:
                        return delegate () { };
                }
            }
        }
    }

    public class Choice
    {
        public string Title;
        public string Subtext;
        public Action OnPressed;
        public Color Color;
        public string ChoiceType;
        public int ChoiceAmount;
        public string ChoiceItem;
        public bool Disabled = false;
        public int ButtonID = PFButton.GetNextID();
        public ChoiceEvent ParentEvent;
        public int Page = -1;

        public SortedDictionary<int, ChoiceChance> Chances = new SortedDictionary<int, ChoiceChance>(new SmallestFirst());
        public int TotalLuckValue = 100;
    }

    public class ChoiceChance
    {
        public Action Trigger;
        public string ChoiceType;
        public int ChoiceAmount;
        public string ChoiceItem;
        public int Page = -1;
    }

    public class SmallestFirst : Comparer<int>
    {
        public override int Compare(int x, int y)
        {
            if(x.CompareTo(y) != 0)
            {
                return x.CompareTo(y);
            } else
            {
                return 0;
            }
        }
    }

    public class SmallestKeyFirst : Comparer<KeyValuePair<int, ChoiceChance>>
    {
        public override int Compare(KeyValuePair<int, ChoiceChance> x, KeyValuePair<int, ChoiceChance> y)
        {
            if(x.Key.CompareTo(y.Key) != 0)
            {
                return x.Key.CompareTo(y.Key);
            } else
            {
                return 0;
            }
        }
    }

    public class ChoiceEvent : CustomEvent
    {
        public List<Choice> Choices = new List<Choice>();
    }

    public class FileChoiceEvent
    {
        public string title;
        public string content;
        public FileChoice[] choices;
    }

    public class FileChoice
    {
        public string title;
        public string subtext;
        public string color;

        public FileChanceChoice[] chances;
    }

    public class FileChanceChoice
    {
        public int chance = -1;
        public string type = "upinf";
        public int amount = 0;
        public string item = "";
    }
}
