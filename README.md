# Hollow Zero
Eh-na-nae~! (Turning Hacknet into a roguelike~!)

---

## What is Hollow Zero?
Beyond being a reference, it's a mod for Hacknet that turns it into a roguelike.

## How?
An economy, events, infections, malware... you name it.

## Can I make an extension with this?
Sure, go hog wild.

## Is everything randomly generated?
If you want it to be. You can change Hollow Zero's mode in your extension config file. If set to `Story`, then you can define your own layers and end point.

## How can I add new events, etc.?
There's two ways to do so:
### Config File
This is recommended for those that just want to add some quick events or change the likelihood of events. You can add custom events in `HZConfig` with XML, rather than needing to use C#.

#### Pros
* No programming required
* Quick, easy to edit
#### Cons
* Extremely limited
* No support for custom malware/modifications/corrutpions/etc. Only events.

### "Hollow Packs"
**Hollow Packs** are tiny little DLLs for `HollowZero` that use C# to extend capabilities for those who want to add extra things to it. Unlike Plugins/Mods, Hollow Packs are much more lightweight, and guaranteed to be compatible with other mods.

#### Pros
* Extended capabilities - add new events, malware, mods, corruptions, whatever.
* Lightweight, don't take much space
#### Cons
* Requires knowledge of C# (albeit, very surface knowledge)