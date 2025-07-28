using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins.Records;
using TrueUnleveledSkyrim.Config;
using System.Collections.Immutable;
using System.Linq;

namespace TrueUnleveledSkyrim.Patch
{
    internal static class OutfitsPatcher
    {
        // Replace leveled-item entries in weak/strong outfit copies
        private static bool ReplaceLvliEntries(Outfit outfit, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, bool isWeak)
        {
            var cache = state.LinkCache;
            bool changed = false;

            if (outfit.Items is null) return false;

            for (int i = 0; i < outfit.Items.Count; i++)
            {
                var entry = outfit.Items[i];

                // In newer Mutagen, Outfit.Item has an Item property holding the link
                if (entry.Item.TryResolve(cache, out ILeveledItemGetter? lvli))
                {
                    string postfix = isWeak ? TUSConstants.WeakPostfix : TUSConstants.StrongPostfix;

                    var replacement = state.PatchMod.LeveledItems
                        .FirstOrDefault(x => x.EditorID == lvli.EditorID + postfix);

                    if (replacement is not null)
                    {
                        changed = true;
                        outfit.Items[i] = new Outfit.Item
                        {
                            Item = replacement.ToLink(),
                            ChanceNone = entry.ChanceNone,
                            Count = entry.Count
                        };
                    }
                }
            }

            return changed;
        }

        public static void PatchOutfits(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            uint processed = 0;

            var outfits = state.LoadOrder
                               .PriorityOrder
                               .Outfit()
                               .WinningOverrides()
                               .ToImmutableList();

            foreach (var src in outfits)
            {
                if (src.Items is null) continue;

                var weak = new Outfit(state.PatchMod);
                var strong = new Outfit(state.PatchMod);

                weak.DeepCopyIn(src);
                strong.DeepCopyIn(src);

                weak.EditorID += TUSConstants.WeakPostfix;
                strong.EditorID += TUSConstants.StrongPostfix;

                if (ReplaceLvliEntries(weak, state, true))
                    state.PatchMod.Outfits.Set(weak);

                if (ReplaceLvliEntries(strong, state, false))
                    state.PatchMod.Outfits.Set(strong);

                processed++;
                if (processed % 100 == 0)
                    Console.WriteLine($"Processed {processed} outfits.");
            }

            Console.WriteLine($"Processed {processed} outfits in total.\n");
        }
    }
}
