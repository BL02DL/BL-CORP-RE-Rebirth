using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly SharedActionsSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<NightVisionComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<NightVisionComponent, ToggleNightVisionEvent>(OnToggleNightVision);
        SubscribeLocalEvent<NightVisionComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, NightVisionComponent component, MapInitEvent args)
    {
        _actionContainer.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnToggleNightVision(EntityUid uid, NightVisionComponent component, ToggleNightVisionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ToggleNightVision(uid, component);
    }

    private void ToggleNightVision(EntityUid uid, NightVisionComponent nightvision)
    {
        nightvision.Enabled = !nightvision.Enabled;

        if (_sharedContainer.TryGetContainingContainer(uid, out var container) &&
            _inventory.TryGetSlotEntity(container.Owner, "eyes", out var entityUid) && entityUid == uid)
            UpdateNightVisionEffects(container.Owner, uid, true, nightvision);

        if (TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, nightvision.Enabled ? "on" : null, component: item);
            _clothing.SetEquippedPrefix(uid, nightvision.Enabled ? "on" : null);
        }

        _appearance.SetData(uid, ToggleVisuals.Toggled, nightvision.Enabled);
        OnChanged(uid, nightvision);
        Dirty(uid, nightvision);
    }

    protected virtual void UpdateNightVisionEffects(EntityUid parent, EntityUid uid, bool state, NightVisionComponent? component) { }

    protected void OnChanged(EntityUid uid, NightVisionComponent component)
    {
        _sharedActions.SetToggled(component.ToggleActionEntity, component.Enabled);
    }

    private void AddToggleVerb(EntityUid uid, NightVisionComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new();
        verb.Text = Loc.GetString("toggle-nightvision-verb-get-data-text");
        verb.Act = () => ToggleNightVision(uid, component);
        args.Verbs.Add(verb);
    }

    private void OnGetActions(EntityUid uid, NightVisionComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent {}
