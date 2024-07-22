using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;
using Pathfinder.Util;

using HollowZero.Choices;

using Stuxnet_HN.Extensions;

namespace HollowZero.Daemons.Event
{
    public class ChoiceEventDaemon : EventDaemon
    {
        public ChoiceEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        [XMLStorage]
        public string CEventTitle = "Choice Event";

        [XMLStorage(IsContent = true)]
        public string CEventContent = "Choice Event Content";

        public ChoiceEvent choiceEvent;
        public const int BUTTON_MARGIN = 10;
        public const string DEFAULT_EVENTS_FILE_PATH = "/Config/Events/ChoiceEvents.xml";

        private static List<ChoiceEvent> possibleEvents = new List<ChoiceEvent>();
        public static List<ChoiceEvent> PossibleEvents
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

            //choiceEvent = TestChoiceEvents.choiceEvents.GetRandom();
            choiceEvent = PossibleEvents.GetRandom();

            EventTitle = choiceEvent.Title;
            EventContent = choiceEvent.Content;

            comp.name = "??? UNKNOWN ???";
        }

        public override void navigatedTo()
        {
            base.navigatedTo();

            comp.name = choiceEvent.Title;
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            DrawEventTemplate(bounds);

            int buttonOffset = 75;
            for (var i = choiceEvent.Choices.Count - 1; i > -1; i--)
            {
                var choice = choiceEvent.Choices[i];
                bool disable = false;
                Color buttonColor = choice.Color;

                if(choice.ChoiceType == "takecreds" && HollowZeroCore.PlayerCredits - choice.ChoiceAmount < 0)
                {
                    disable = true;
                    buttonColor = new Color(100, 100, 100);
                }

                foreach(var chance in choice.Chances)
                {
                    if(chance.Value.ChoiceType == "takecreds" && HollowZeroCore.PlayerCredits - chance.Value.ChoiceAmount < 0)
                    {
                        disable = true;
                        buttonColor = new Color(100, 100, 100);
                    }
                }

                string bText = $"{choice.Title}\n{choice.Subtext}";
                var b = Button.doButton(choice.ButtonID, bounds.X + 25,
                    bounds.Y + bounds.Height - buttonOffset, bounds.Width - 50, 50, bText, buttonColor);
                if (b)
                {
                    if(disable)
                    {
                        OS.currentInstance.warningFlash();
                        OS.currentInstance.terminal.writeLine("<!> You don't have enough resources for that!");
                        return;
                    }

                    if(choice.OnPressed != null)
                    {
                        choice.OnPressed.Invoke();
                    } else
                    {
                        int luck = choice.TotalLuckValue;
                        Random random = new Random();
                        int luckValue = random.Next(0, luck);

                        foreach(var chance in choice.Chances)
                        {
                            if(luckValue < chance.Key)
                            {
                                chance.Value.Trigger.Invoke();
                                break;
                            }
                        }
                    }

                    OS.currentInstance.display.command = "probe";
                    RemoveDaemon();
                }
                buttonOffset += 60;
            }
        }

        public const string XML_TAG = "HollowZeroChoiceEvents";

        public static void ReadChoiceEventsFile(string filename = "")
        {
            filename = filename == "" ? ExtensionLoader.ActiveExtensionInfo.FolderPath + DEFAULT_EVENTS_FILE_PATH : filename;
            if (!File.Exists(filename)) { return; }

            Console.WriteLine(HollowZeroCore.HZLOG_PREFIX + "Reading choice events...");

            FileStream eventsFileStream = File.OpenRead(filename);
            XmlReader xml = XmlReader.Create(eventsFileStream);

            List<ChoiceEvent> choiceEvents = new List<ChoiceEvent>();

            while(xml.Name != XML_TAG)
            {
                xml.Read();
                if(xml.EOF)
                {
                    throw new FormatException(HollowZeroCore.HZLOG_PREFIX + $"Unexpected end of file looking for {XML_TAG} tag.");
                }
            }

            do
            {
                xml.Read();

                if(xml.Name == XML_TAG && !xml.IsStartElement())
                {
                    PossibleEvents = choiceEvents;
                    xml.Close();
                    Console.WriteLine(HollowZeroCore.HZLOG_PREFIX + "Choice events successfully added.");
                    return;
                }

                if(xml.Name == "ChoiceEvent" && xml.IsStartElement())
                {
                    ChoiceEvent ev = new ChoiceEvent()
                    {
                        Title = xml.ReadRequiredAttribute("Title")
                    };

                    do
                    {
                        xml.Read();

                        if(xml.Name == "Content" && xml.IsStartElement())
                        {
                            ev.Content = xml.ReadElementContentAsString();
                        }

                        if(xml.Name == "Choice" && (xml.IsEmptyElement || xml.IsStartElement()))
                        {
                            Choice choice = ReadChoiceXML(xml);
                            ev.Choices.Add(choice);
                        }
                    } while (xml.Name != "ChoiceEvent");

                    Console.WriteLine(ev.Title);
                    choiceEvents.Add(ev);
                }
            } while (!xml.EOF);
            throw new FormatException(HollowZeroCore.HZLOG_PREFIX + "Unexpected end-of-file while reading Hollow Zero choice events XML!");

            Choice ReadChoiceXML(XmlReader xml)
            {
                bool hasChances = !xml.IsEmptyElement;
                Choice choice = new Choice()
                {
                    Title = xml.ReadRequiredAttribute("Title"),
                    Subtext = xml.ReadRequiredAttribute("Subtext"),
                    Color = new Color().FromString(xml.ReadRequiredAttribute("Color"))
                };

                if (!hasChances)
                {
                    string type = xml.ReadRequiredAttribute("Type");
                    choice.ChoiceType = type;
                    int amount = 0;
                    string item = "";

                    if (xml.MoveToAttribute("Amount"))
                    {
                        amount = int.Parse(xml.ReadContentAsString());
                    }

                    if (xml.MoveToAttribute("Item"))
                    {
                        item = xml.ReadContentAsString();
                    }

                    choice.ChoiceAmount = amount;
                    choice.ChoiceItem = item;

                    choice.OnPressed = DetermineActionFromType(type, amount, item);
                } else
                {
                    int luck = 0;
                    List<KeyValuePair<int, ChoiceChance>> percentChanceList = new List<KeyValuePair<int, ChoiceChance>>();
                    do
                    {
                        xml.Read();
                        Console.WriteLine(xml.Name);

                        if (xml.Name == "ChoiceChance")
                        {
                            ChoiceChance chance = new ChoiceChance();

                            int chancePercent = int.Parse(xml.ReadRequiredAttribute("Chance"));
                            string type = xml.ReadRequiredAttribute("Type");
                            int amount = 0;
                            string item = "";

                            if (xml.MoveToAttribute("Amount"))
                            {
                                amount = int.Parse(xml.ReadContentAsString());
                            }

                            if (xml.MoveToAttribute("Item"))
                            {
                                item = xml.ReadContentAsString();
                            }

                            Action action = DetermineActionFromType(type, amount, item);

                            chance.ChoiceType = type;
                            chance.ChoiceAmount = amount;
                            chance.ChoiceItem = item;
                            chance.Trigger = action;

                            percentChanceList.Add(new KeyValuePair<int, ChoiceChance>(chancePercent, chance));
                        }
                    } while (xml.Name != "Choice");

                    percentChanceList.Sort(new SmallestKeyFirst());
                    foreach(var pChance in percentChanceList)
                    {
                        luck += pChance.Key;
                        choice.Chances.Add(luck, pChance.Value);
                    }

                    choice.TotalLuckValue = luck;
                }

                return choice;
            }

            Action DetermineActionFromType(string type, int amount = 0, string item = "")
            {
                switch(type)
                {
                    case "upinf":
                        return delegate () { HollowZeroCore.IncreaseInfection(amount); };
                    case "downinf":
                        return delegate () { HollowZeroCore.DecreaseInfection(amount); };
                    case "clearinf":
                        return delegate () { HollowZeroCore.ClearInfection(); };
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

        public SortedDictionary<int, ChoiceChance> Chances = new SortedDictionary<int, ChoiceChance>(new SmallestFirst());
        public int TotalLuckValue = 100;
    }

    public class ChoiceChance
    {
        public Action Trigger;
        public string ChoiceType;
        public int ChoiceAmount;
        public string ChoiceItem;
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

    public class ChoiceEvent
    {
        public string Title;
        public string Content;
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
