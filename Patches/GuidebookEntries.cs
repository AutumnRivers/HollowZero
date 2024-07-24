using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Patches
{
    internal static class DefaultGuidebookEntries
    {
        /*internal static List<string> GuidebookEntryTitles = new List<string>()
        {
            "Basics - Basic Gameplay", "Basics - Infection", "Basics - Mods/Corruptions", "Basics - Fail States",
            "Event - Dialogue", "Event - Choice", "Event - Chance", "Event - Unavoidable", "Event - Story",
            "Econ. - Credits", "Econ. - Shops", "Econ. - Trinity", "Econ. - Antivirus", "Econ. - Gacha",
            "Networks - Basics",
            "Malware - Types",
            "Misc. - Rest Stops", "Misc. - Assistants", "Misc. - Rivals"
        };*/

        internal static readonly List<GuidebookEntry> entries = new List<GuidebookEntry>()
        {
            new GuidebookEntry()
            {
                Title = "Gameplay Basics",
                Content = "Hollow Zero is very akin to most roguelikes you may have played. " +
                "Additionally, it also plays very similar to base Hacknet. Hack through nodes, find the end. " +
                "However, in Hollow Zero, each Layer is randomly generated.\n\n" +
                "Unless the extension is in \"Story Mode\", then there is no definitive end to the gameplay. " +
                "Simply survive for as long as you can!",
                ShortTitle = "Basics - Basic Gameplay"
            },
            new GuidebookEntry()
            {
                Title = "Fail States",
                Content = "Unless \"Story Mode\" is enabled in the extension, then Hollow Zero has multiple fail states:\n" +
                "1. Gain four malware and fail to remove at least one within the time limit\n" +
                "2. Gain another piece of malware while having four active malware\n" +
                "3. Fail to escape a Timed Layer before the time limit runs out\n" +
                "4. Fail to kill a forkbomb on your machine before it finishes\n\n" +
                "If Story Mode is enabled, then there is either a) no fail state, or " +
                "b) only fail state(s) defined by the extension developer.\n\n" +
                "In these cases, anything above that would normally cause a run to end will instead trigger ETAS.",
                ShortTitle = "Basics - Fail States"
            },
            new GuidebookEntry()
            {
                Title = "Infection & Malware",
                Content = "Infection can be gained in numerous ways. The most common way is with events. " +
                "However, Infection can also be gained from Corruptions or Unstable Layers. " +
                "When your Infection reaches 100 or above, then it will be reset, but you will also gain random Malware.\n\n" +
                "-= What is Malware? =-\n" +
                "Malware is a persistent negative effect that can do anything from prolong proxy times on every node " +
                "to randomly steal credits from you every few minutes. Malware, once gained, cannot be removed " +
                "except for in very specific circumstances, such as certain events.\n\n" +
                "Keep an eye on your Infection, or you might end up with more than you can handle...",
                ShortTitle = "Basics - Infection"
            },
            new GuidebookEntry()
            {
                Title = "Modifications & Corruptions",
                Content = "-= Modifications =-\n" +
                "Modifications are persistent positive effects that aid you through your run. " +
                "They can automatically PortHack for you, randomly kill incoming Forkbombs, and much more!\n" +
                "Modifications can be gained through Shops and Events.\n\n" +
                "-= Corruptions =-\n" +
                "Corruptions are persistent EXTREMELY negative (but temporary) effects that will no doubt hinder your progress. " +
                "They might force you into terminal mode, randomly temporarily disable commands, and much more.\n" +
                "Corruptions are gained through Events and Malware.",
                ShortTitle = "Basics - Mods"
            },
            new GuidebookEntry()
            {
                Title = "Dialogue Events",
                Content = "Events where a piece of dialogue is shown. Rarely used outside of Story Mode.",
                ShortTitle = "Events - Dialogue"
            },
            new GuidebookEntry()
            {
                Title = "Choice Events",
                Content = "Events where the player is prompted to make a choice. " +
                "Choices can be detrimental or helpful towards a player's progress. Additionally, " +
                "some choices might have random outcomes. So, they COULD do something good, " +
                "or they could do something bad.\n\n" +
                "Make your decisions wisely. You can't take them back, after all.",
                ShortTitle = "Events - Choice"
            },
            new GuidebookEntry()
            {
                Title = "Chance Events",
                Content = "Events where the player is prompted to take a random chance. " +
                "Similar to Choice Events, but completely centered around taking chances.\n\n" +
                "Roll the dice. What's the worst that could happen?",
                ShortTitle = "Events - Chance"
            },
            new GuidebookEntry()
            {
                Title = "Unavoidable Events",
                Content = "Events that, once shown, the player *MUST* interact with to continue.\n\n" +
                "Most events can be ignored or have an option to leave them, allowing the player to continue " +
                "to the node. Unavoidable Events must be interacted with, locking the rest of the game up " +
                "until it is interacted with.\n\n" +
                "Most of these types of events have negative effects. Be on the lookout!",
                ShortTitle = "Events - Unavoidable"
            },
            new GuidebookEntry()
            {
                Title = "Story Events",
                Content = "Most often Dialogue Events with multiple pages of dialogue.\n\n" +
                "Story Events only appear when \"Story Mode\" is enabled in the HZ config by the extension developer.",
                ShortTitle = "Events - Story"
            },
            new GuidebookEntry()
            {
                Title = "Credits",
                Content = "The butter to the bread of the economy in Hollow Zero.\n\n" +
                "Credits can be used in Shops to purchase Modifications, Programs, and decrease Infection or remove Malware. " +
                "(Only in certain shops.)\n\n" +
                "Credits can also be used in certain events to avoid negative choices. It's good to keep them on hand!",
                ShortTitle = "Econ. - Credits"
            },
            new GuidebookEntry()
            {
                Title = "Shops",
                Content = "The bread to the butter of credits.\n\n" +
                "Shops can contain Modifications and Programs. Certain shops may also have ways to decrease Infection " +
                "or remove Malware as sale options. Multiple shop types exist, so keep an eye on any you may need.",
                ShortTitle = "Econ. - Shops"
            },
            new GuidebookEntry()
            {
                Title = "Trinity, Prof. Pentester",
                Content = "Trinity is a black-hat-hacker-turned-pentester who aids hackers in return for credits.\n\n" +
                "Unlike shops, Trinity's sale options are all chance-based, meaning you don't know what purchasing " +
                "an item from Trinity will do beforehand. Her options *are* cheap, though... maybe worth the risk?",
                ShortTitle = "Econ. - Trinity"
            },
            new GuidebookEntry()
            {
                Title = "A.I.-ntivirus",
                Content = "A.I.A.V. (A.I. AntiVirus) is a sentient AI with Antivirus capibalities.\n\n" +
                "By paying Aive (as it goes by) credits, you can remove Malware or completely clear your " +
                "Infection level. Aive can also remove a Corruption early for you, but its methods are still... " +
                "unstable. Attempting to remove a Corruption may just make it worse.\n\n" +
                "Give it a break, it's trying its best.",
                ShortTitle = "Econ. - Aive"
            },
            new GuidebookEntry()
            {
                Title = "Gacha Shops",
                Content = "Gacha Shops will give you a random Modification in return for credits. Additionally, " +
                "you can use these shops to upgrade a random Modification you already have. It could upgrade " +
                "a Modification you want it to, or one you couldn't care less for.\n\n" +
                "\"Let's go gambling! Aw, dangit! Aw, dangit! Aw, dan--\"",
                ShortTitle = "Econ. - Gacha"
            },
            new GuidebookEntry()
            {
                Title = "Rest Stops",
                Content = "Decreases your Infection Level by a bit.",
                ShortTitle = "Misc. - Rest Stops"
            },
            new GuidebookEntry()
            {
                Title = "Support Centers",
                Content = "Support Centers are where you can find supposed tech support agents. " +
                "While some centers are genuine, others are fake, and will only do harm to your PC. " +
                "You won't know whether it's genuine or not, unless you take the risk.\n\n" +
                "-= Genuine Centers =-\n" +
                "Genuine support centers will either remove 1 Malware or clear your Infection level for free. " +
                "No catch.\n\n" +
                "-= Fake Centers =-\n" +
                "Fake support centers will do damage to your PC. At their best, they'll only raise your " +
                "Infection level by a generous amount. At their worst... they could steal a Program or forcibly " +
                "add Malware to your system.\n\n" +
                "\"Hello, yes? This is Moonshine Support. Totally real. Mhm.\"",
                ShortTitle = "Misc. - Support"
            },
            new GuidebookEntry()
            {
                Title = "Types of Malware",
                Content = "Malware comes in many different types. Here's a handy guide to them:\n" +
                "-= Cryptominer =-\n" +
                "-100 Max RAM until removed.\n\n" +
                "-= Ransomware =-\n" +
                "Encrypts a random file on your system every 60 seconds. Files are decrypted after 90 seconds. " +
                "If you're in a rush, you can also run \"payrs\", which will make you lose 100 credits, " +
                "but it immediately decrypts all your files.\n\n" +
                "-= Botnet =-\n" +
                "Randomly get attacked by outside nodes.\n\n" +
                "-= Adware =-\n" +
                "Random chance to lose 25 credits every time you trigger an event.\n\n" +
                "-= Spyware =-\n" +
                "Closes a random port on the node you're currently connected to at a random interval.\n\n" +
                "-= Dropper =-\n" +
                "Every node, random chance to gain a random Corruption.\n\n" +
                "-= Packet Storm =-\n" +
                "Prolongs proxy times by x1.2\n\n" +
                "-= FireScramble =-\n" +
                "When a node is generated, random chance for it to have a randomly generated firewall.",
                ShortTitle = "Malware - Types"
            }
        };
    }
}
