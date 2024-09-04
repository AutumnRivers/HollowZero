using Hacknet;
using Hacknet.Effects;

using HollowZero.Daemons.Event;

using HollowZero.Nodes;
using HollowZero.Nodes.LayerSystem;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using System.Collections.Generic;
using System.Linq;

using BepInEx;

using static HollowZero.HollowLogger;
using Pathfinder.Util;

namespace HollowZero.Managers
{
    public static class GameplayManager
    {
        public static void AddChoiceEvent(ChoiceEvent ev)
        {
            ChoiceEventDaemon.PossibleEvents.Add(ev);
        }

        public static void AddChoiceEvent(IEnumerable<ChoiceEvent> evs)
        {
            ChoiceEventDaemon.PossibleEvents.AddRange(evs);
        }

        public static void ForkbombComputer(Computer target)
        {
            Multiplayer.parseInputMessage($"eForkBomb {target.ip}", OS.currentInstance);
        }

        internal static void DrawTrueCenteredText(Rectangle bounds, string text, SpriteFont font, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, textColor);
        }

        internal static void DrawFlickeringCenteredText(Rectangle bounds, string text, SpriteFont font, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            Rectangle container = new Rectangle()
            {
                X = (int)textPosition.X,
                Y = (int)textPosition.Y,
                Width = (int)textVector.X,
                Height = (int)textVector.Y
            };

            FlickeringTextEffect.DrawFlickeringText(container, text, 5f, 0.35f, font, OS.currentInstance, textColor);
        }

        internal static void LoadInLayer(HollowLayer layerData)
        {
            NodeManager.ClearNetMap();
            OS.currentInstance.delayer.Post(ActionDelayer.NextTick(), actuallyLoadInLayer);

            void actuallyLoadInLayer()
            {
                Dictionary<string, int> nodesOnNetmap = new();
                string firstNodeID = "";

                foreach (var node in layerData.nodes)
                {
                    int nodeIndex = NodeManager.AddNode(node);
                    if (!nodesOnNetmap.Any()) firstNodeID = node.idName;
                    LogDebug($"Adding node (ID:{node.idName})(IP:{node.ip})", true);
                    node.links.Clear();
                    nodesOnNetmap.Add(node.idName, nodeIndex);
                }
                foreach (var node in layerData.nodes.Where(n => !n.attatchedDeviceIDs.IsNullOrWhiteSpace()))
                {
                    var ids = node.attatchedDeviceIDs;
                    bool sep = ids.Contains(",");

                    List<string> nodeIDs = new();
                    if (sep)
                    {
                        nodeIDs = ids.Split(',').ToList();
                    }
                    else
                    {
                        nodeIDs.Add(ids);
                    }
                    node.links.Clear();
                    foreach (var id in nodeIDs)
                    {
                        node.links.Add(nodesOnNetmap[id]);
                    }

                    LogDebug($"Replacing node (ID:{node.idName}) links with updated indexes", true);
                    NodeManager.ReplaceNode(node.idName, node);
                }
                NodeManager.SetNodeVisibility(firstNodeID, true);
                ComputerLookup.RebuildLookups();
            }
        }

        internal static void GenerateAndLoadInLayer()
        {
            var layer = LayerGenerator.GenerateSolvableLayer();
            LoadInLayer(layer);
        }
    }
}
