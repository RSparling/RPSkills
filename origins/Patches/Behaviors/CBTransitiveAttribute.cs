﻿using HarmonyLib;
using Newtonsoft.Json.Linq;
using Origins.Systems;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Origins.Patches.Behaviors
{
    internal class CBTransitiveAttribute : CollectibleBehavior, ICodePatch
    {
        static readonly string[] PatchedClasses = new string[] { "ItemPlantableSeed" };
        static readonly string[] attr_list = new string[] { "mutationRate" };
        static readonly string attr_list_name = "transitiveAttributes";

        static ICoreAPI api;
        private double mutation = 1.0f;
        private Harmony patch;

        public CBTransitiveAttribute(CollectibleObject collObj) : base(collObj)
        {
        }

        /// <summary>
        /// Mostly used for manual initialization but also called when JSON patch applies this behavior.
        /// </summary>
        /// <param name="properties">will only have values when JSON patches apply this behavior</param>
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            collObj.Attributes ??= properties;

            OriginsLogger.Debug(api, "[CBTransitiveAttribute] Initializing" + collObj.Code);
            OriginsLogger.Debug(api, "properties:");
            if (properties.KeyExists(attr_list_name)) OriginsLogger.Debug(api, "  attr_list_name: " + properties[attr_list_name]);
            if (properties.KeyExists(attr_list[0])) OriginsLogger.Debug(api, "  " + attr_list[0] + ": " + properties[attr_list[0]]);
            OriginsLogger.Debug(api, "collObj.Attributes:");
            if (properties.KeyExists(attr_list_name)) OriginsLogger.Debug(api, "  attr_list_name: " + collObj.Attributes[attr_list_name]);
            if (properties.KeyExists(attr_list[0])) OriginsLogger.Debug(api, "  " + attr_list[0] + ": " + collObj.Attributes[attr_list[0]]);

            mutation = properties[attr_list[0]].AsDouble(0.0d);

        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            OriginsLogger.Debug(byEntity.Api, "Attacking using an item with transitive properties!");

            PatchDebugger.PrintDebug(byEntity.Api, slot.Itemstack.Attributes);

            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            byEntity.Api.Logger.Debug("Interact Start");
            byEntity.Api.Logger.Debug(slot.Itemstack.GetName());
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine("Mutation Rate: " + inSlot.Itemstack.Attributes[attr_list[0]]);
        }


        public static void ApplyPatch(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server)
            {
                return;
            }

            foreach (CollectibleObject item in api.World.Collectibles)
            {
                // first two are necessary to make sure it exists, third is for a robust method of filtering
                if (item == null || item.Code == null || item.Class == null)
                {
                    continue;
                }

                if (item.Code.BeginsWith("game", "seeds"))
                {
                    CBTransitiveAttribute behavior = new CBTransitiveAttribute(item);

                    item.Attributes ??= new JsonObject(new JObject());

                    item.Attributes.Token[attr_list_name] = JToken.FromObject(attr_list);
                    foreach (string attrKey in attr_list)
                    {
                        // needs default init values if none are given by properties
                        item.Attributes.Token[attrKey] = JToken.FromObject(1.0d);
                    }

                    behavior.Initialize(item.Attributes);

                    item.CollectibleBehaviors = item.CollectibleBehaviors.Append(behavior);
                }
            }
        }

        public static void RegisterPatch(ICoreAPI api)
        {
            CBTransitiveAttribute.api = api;

            OriginsLogger.Debug(api, "[CBTransitiveAttribute] Registering patch: CBTransitiveAttribute");

            api.RegisterCollectibleBehaviorClass("CBTransitiveAttribute", typeof(CBTransitiveAttribute));
        }
    }
}
