using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Starfield;
using Mutagen.Bethesda.Plugins;
using Noggog;

namespace StarfieldDelevelled
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<IStarfieldMod, IStarfieldModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.Starfield, "Delevelers.esp")
                .Run(args);
        }
        public static void RunPatch(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
        {
            Console.WriteLine("Running PatchObjectModifications...");
            PatchObjectModifications(state);
            Console.WriteLine("Finished PatchObjectModifications.");

            Console.WriteLine("Running PatchConstructibleObjects...");
            PatchConstructibleObjects(state);
            Console.WriteLine("Finished PatchConstructibleObjects.");
        }

        private static void PatchObjectModifications(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
        {
            foreach (var omodGetter in state.LoadOrder.PriorityOrder.AObjectModification().WinningOverrides())
            {
                if (omodGetter.Includes.Count == 0) continue;

                foreach (var inc in omodGetter.Includes)
                {
                    if (inc.MinimumLevel <= 1) continue;
                    var omod = state.PatchMod.ObjectModifications.GetOrAddAsOverride(omodGetter);
                    var incIndex = omod.Includes.IndexOf(inc);
                    omod.Includes[incIndex].MinimumLevel = 1;
                }
            }
        }

        private static void PatchConstructibleObjects(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
        {
            foreach (var cobjGetter in state.LoadOrder.PriorityOrder.ConstructibleObject().WinningOverrides())
            {
                if (cobjGetter.Conditions.Count == 0) continue;

                foreach (var cobjGetterCondition in cobjGetter.Conditions)
                {
                    if (cobjGetterCondition is not { Data: IGetLevelConditionDataGetter refData } ||
                        refData.Reference.FormKey != FormKey.Factory("000014:Starfield.esm")) continue;
                    var condFloat = (IConditionFloatGetter)cobjGetterCondition;
                    if (condFloat.ComparisonValue.EqualsWithin(1) ||
                        (condFloat.CompareOperator != CompareOperator.GreaterThan &&
                         condFloat.CompareOperator != CompareOperator.GreaterThanOrEqualTo)) continue;
                    var cobj = state.PatchMod.ConstructibleObjects.GetOrAddAsOverride(cobjGetter);
                    var index = cobjGetter.Conditions.IndexOf(cobjGetterCondition);
                    ((IConditionFloat)cobj.Conditions[index]).ComparisonValue = 1;
                }
            }
        }
    }
}