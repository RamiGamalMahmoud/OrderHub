# MudNavGroup Collapse After Navigation — Investigation Result

## 1. Executive summary

**Confirmed source-level cause: the route-dependent key on the outer PermissionErrorBoundary recreates the entire layout subtree.** In [OrderHub.Web/Components/Routes.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Routes.razor:9), `@key="routeData"` surrounds AuthorizeRouteView, which owns MainLayout. Navigation supplies a different RouteData object; that changes the key and replaces MainLayout, MudDrawer, NavMenu and all four MudNavGroup instances. The new groups start collapsed.

This is component recreation, rather than an expanded group merely being rerendered. It also explains why expansion booleans bound inside NavMenu or MainLayout would reset: those owners are replaced too.

The smallest appropriate future fix is to preserve the boundary/layout identity during ordinary routing and reset a faulted error boundary through explicit navigation recovery. Keep normal MudNavGroup state and the existing responsive drawer. **No fix was implemented.**

Evidence establishes the deterministic lifecycle defect in this checkout. A live authenticated navigation trace was unavailable, so this report does not claim browser-observed component IDs or viewport reproduction.

- Investigated: 2026-10-06.
- Branch: `feature/web-app`.
- HEAD: `d34fe2e68597295669836ae4fdd2fa95724ccb80`.
- Initial working tree: clean.
- Final source/staged changes: none. The requested report is the sole added file.

## 2. Current behavior and reproduction path

The reported path is: open a normal application page, expand **جهات التعامل**, click **الموردون** at `/suppliers`, wait for navigation, and observe the group collapsed.

Current NavMenu defines products, contacts, documents and administration groups. None has Expanded/ExpandedChanged binding, an explicit expansion value, a route-derived expansion rule or a custom child-click collapse handler. MudNavMenu also leaves MultiExpansion at its library default. Links have ordinary Href values; the selected Supplier page uses MainLayout through the default routing layout.

Predicted sequence for successful interactive navigation:

1. The existing contacts group holds its user-expanded state.
2. The child link initiates navigation.
3. Router supplies the new route to Routes.
4. The route-dependent outer key changes.
5. The old boundary and its descendants are removed; new instances are created.
6. The newly created group starts with its default collapsed state.

This affects ordinary successful navigation; no permission failure, authentication change or mobile breakpoint is needed.

## 3. Repository files inspected

| File | Relevant finding |
|---|---|
| [OrderHub.Web/Components/Routes.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Routes.razor:9) | RouteData key above AuthorizeRouteView and MainLayout |
| [OrderHub.Web/Components/App.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/App.razor:18) | Global InteractiveServer Routes; Blazor Web App script; viewport metadata |
| [OrderHub.Web/Program.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Program.cs) | Interactive Server registration/mapping, Mud services, cookie auth and cascading authentication |
| [OrderHub.Web/Components/Layout/MainLayout.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Layout/MainLayout.razor:36) | Responsive drawer; NavMenu is an unconditional child |
| [OrderHub.Web/Components/Layout/NavMenu.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Layout/NavMenu.razor) | Four groups; PermissionView wraps individual links |
| [OrderHub.Web/Components/Common/PermissionErrorBoundary.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Common/PermissionErrorBoundary.cs:11) | AuthorizationDeniedException redirects; otherwise inherited error handling |
| [OrderHub.Web/Components/Common/PermissionView.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Common/PermissionView.razor:14) | Async permission evaluation in OnParametersSetAsync |
| [OrderHub.Web/Services/PermissionAccess.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Services/PermissionAccess.cs) | Existing executor-backed permission query |
| [OrderHub.Web/Services/AuthenticatedUser.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Services/AuthenticatedUser.cs) | Reads authentication state; does not publish state changes |
| [OrderHub.Web/Services/AuthenticatedRequestExecutor.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Services/AuthenticatedRequestExecutor.cs) | Isolated operation scope per request |
| [OrderHub.Web/Services/EditorServiceRegistration.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Services/EditorServiceRegistration.cs) | Existing scoped identities/executor registrations |
| [OrderHub.Web/Infrastructure/Authorization/ServiceCollectionExtensions.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Infrastructure/Authorization/ServiceCollectionExtensions.cs) | PermissionAccess and authorization policies |
| [OrderHub.Web/Infrastructure/Authorization/PermissionRequirement.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Infrastructure/Authorization/PermissionRequirement.cs) | Route permission evaluation |
| [OrderHub.Web/Components/Common/RedirectToLogin.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Common/RedirectToLogin.razor) | Redirect for unauthenticated routing |
| [OrderHub.Web/Components/Layout/StandaloneLayout.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Layout/StandaloneLayout.razor) | Different layout for login/setup/not-found pages |
| [OrderHub.Web/Components/Pages/Features/Authentication/Login.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Authentication/Login.razor) | Explicit StandaloneLayout |
| [OrderHub.Web/Components/Pages/Features/Suppliers/Index.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Suppliers/Index.razor) | Supplier route and permission policy; no separate render mode/layout |
| [OrderHub.Web/wwwroot/app.css](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/wwwroot/app.css:47) | Modern nav styling without group-collapse logic |
| [OrderHub.Web/Components/Layout/NavMenu.razor.css](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Layout/NavMenu.razor.css) | Legacy scoped navbar selectors |
| [OrderHub.Web/Components/Layout/MainLayout.razor.css](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Layout/MainLayout.razor.css) | Legacy layout selectors and error UI |
| [OrderHub.Web/OrderHub.Web.csproj](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/OrderHub.Web.csproj) | net9.0; MudBlazor 9.11.0 |
| [OrderHub.Web/obj/project.assets.json](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/obj/project.assets.json) | Resolved MudBlazor/9.11.0 |
| [OrderHub.Web/Properties/launchSettings.json](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Properties/launchSettings.json) | Local HTTP port 5072 |

Also inspected local NuGet 9.11.0 XML documentation and Git history for Routes. Commit `f8e7692` (`feat(auth): implement role-based authorization`) introduced both the custom boundary and its RouteData key. That identifies the origin of the lifecycle change, although no historical browser behavior was measured.

## 4. Component lifecycle findings

### Why this key replaces the layout

The .NET 9 Router's matched-route path constructs RouteData and passes it to Found. A read-only reflection check of the installed ASP.NET Core 9.0.20 Components assembly confirmed that RouteData inherits Object.Equals; two separately constructed RouteData objects for the same page and values compare unequal. Thus even equal route contents do not make this an identity-stable key. [Router source](https://raw.githubusercontent.com/dotnet/aspnetcore/v9.0.0/src/Components/Components/src/Routing/Router.cs).

Blazor's keyed diff compares key equality. A replacement key causes removal/insertion rather than reuse, including descendant component disposal. The application's key is above the component that renders the layout, so preserving MainLayout's type cannot preserve its instance across that replacement. [Keyed diff implementation](https://raw.githubusercontent.com/dotnet/aspnetcore/v9.0.0/src/Components/Components/src/RenderTree/RenderTreeDiffBuilder.cs), [official key semantics](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/element-component-model-relationships?view=aspnetcore-9.0#when-to-use-key).

| Component/state | Current successful navigation consequence |
|---|---|
| Interactive Routes/Router | Processes new location; does not need a new circuit |
| PermissionErrorBoundary | Replaced because its key changed |
| AuthorizeRouteView and layout descendants | Removed with the old keyed owner |
| MainLayout | New instance; local fields initialize again |
| MudDrawer | New instance; previous open state is lost |
| NavMenu | New instance; previous local/group state is lost |
| MudNavGroup | New instance; default expansion is false |
| PermissionView/child links | New instances; permissions evaluate again |
| Theme/popover/dialog/snackbar providers in MainLayout | Also sit inside the replaced subtree |

The renderer discards component instances whether or not an application component itself implements IDisposable. MudNavGroup has disposal logic; NavMenu/MainLayout need no custom disposal method to lose their fields.

### Expected normal Blazor behavior

With stable route-view ownership and the same layout type, RouteView renders LayoutView at a stable position and updates its page content; a new page does not inherently require a new shell. The current configuration overrides that opportunity with the outer key. [RouteView source](https://raw.githubusercontent.com/dotnet/aspnetcore/v9.0.0/src/Components/Components/src/RouteView.cs).

App uses InteractiveServer on Routes and HeadOutlet, and Program registers/maps that mode. There is no per-page render-mode boundary on the example destination. Initial prerender-to-interactive startup can create fresh instances once; it is separate from repeated child-link navigation after interactivity starts. Login/StandaloneLayout, a full reload or a new circuit are also separate cases.

## 5. MudBlazor 9.11.0 findings

These findings use the exact `v9.11.0` source, corroborated by the resolved local package, rather than development-branch behavior.

- **MudNavMenu:** MultiExpansion defaults to true. Exclusive expansion only collapses other registered groups when a user expands a group with MultiExpansion=false. The application does not select that option. [9.11.0 menu source](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/NavMenu/MudNavMenu.razor.cs).
- **MudNavGroup:** Expanded defaults to false and is managed through ParameterState with ExpandedChanged. Header toggling changes that state; the group does not subscribe to route changes or collapse itself on a child navigation. Its header button and child content are separate DOM regions, so a child click is not a header toggle. [Group logic](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/NavMenu/MudNavGroup.razor.cs), [group markup](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/NavMenu/MudNavGroup.razor).
- **Rerender distinction:** ParameterState initializes for a new instance and tracks later supplied parameter changes. An unchanged external expansion parameter does not itself overwrite a user-updated internal value on every ordinary parameter pass. [Parameter-state implementation](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/State/ParameterStateInternalOfT.cs).
- **MudNavLink:** The normal Href branch renders Blazor NavLink and informs its cascading INavigationEventReceiver on click. In this hierarchy that receiver is the drawer. This notification does not call MudNavGroup.CollapseAsync. The library's ForceLoad default is false, and the application supplies no forced-navigation handlers on these links. [Link logic](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/NavMenu/MudNavLink.razor.cs), [link markup](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/NavMenu/MudNavLink.razor).
- **MudDrawer:** Its navigation receiver closes Temporary drawers, and Responsive drawers only below their resolved breakpoint. This changes drawer Open state, not group Expanded state. [Drawer logic](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/Drawer/MudDrawer.razor.cs).
- **Drawer content:** ChildContent is rendered unconditionally inside the aside; closing/hiding the drawer does not itself remove NavMenu. Reopening the same drawer can therefore retain its groups. The application instead replaces the drawer through the outer route key. [Drawer markup](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/Drawer/MudDrawer.razor).

Binding Expanded is supported; the symptom does not show that MudBlazor ignores binding. Binding to a field whose containing component is recreated cannot preserve the old field.

## 6. Responsive drawer and breakpoint findings

MainLayout explicitly uses Responsive and does not override Breakpoint. The package default is Md. Local 9.11.0 documentation gives Md=960, Sm=600, Lg=1280 and Xl=1920 CSS pixels; AddMudServices and the theme contain no custom breakpoint definitions.

Responsive remains that enum value, with docked behavior at/above the threshold and overlay behavior below it. It is not literally rewritten to Temporary for this variant. Temporary-like mobile closing is determined by the drawer's resolved viewport condition. Initial viewport detection and crossing the threshold can also update Open. [Exact drawer implementation](https://raw.githubusercontent.com/MudBlazor/MudBlazor/v9.11.0/src/MudBlazor/Components/Drawer/MudDrawer.razor.cs).

| CSS viewport | Expected drawer behavior after viewport detection | Expected effect of current route key |
|---|---|---|
| 1366×768 | Docked laptop drawer | New menu/groups after route change |
| 1920×1080 | Docked wider desktop drawer | Same replacement |
| 390×844 | Mobile overlay; destination selection closes it | Menu state also lost through layout replacement |
| Width 959 | Below threshold | Replacement remains |
| Width 960 | At threshold, docked | Replacement remains |

These are **source-derived expectations, not live observations**. Browser zoom or a reduced content viewport can make a physically wide screen fall below the CSS threshold.

Runtime attempt: available browser inventory contained no existing OrderHub session. Opening the configured local HTTP address returned ERR_CONNECTION_REFUSED. No app was started, credentials changed or debugging source added. Consequently the requested live widths, DOM node identities and circuit events could not be measured.

Mobile drawer closure is appropriate UX and should remain independent from desktop group preservation. With a stable layout, hiding/reopening the mobile drawer should not by itself reset group expansion.

## 7. PermissionView findings

PermissionView initializes `_allowed` to false. Its OnParametersSetAsync awaits the existing permission query and then assigns the result. It does **not** reset `_allowed` to false at the start of each check. A currently permitted child's content therefore remains eligible for rendering while a later check is pending, unless the actual result changes.

A newly created PermissionView starts false and adds its link after permission evaluation. That initial omission can make links appear asynchronously, but its parent MudNavGroup is outside PermissionView in every current group. PermissionView can remove/recreate an individual link when its result changes; it does not surround or own the group/header.

The current route key recreates these views, so their checks repeat as a consequence. Even after preserving layout identity, OnParametersSetAsync may run again when ancestors supply child fragments; PermissionAccess has no caching. That alone does not reset the containing group's state.

AuthorizeView in administration wraps individual links too. Route authorization and authentication changes can alter content, but AuthenticatedUser reads rather than publishes authentication state, and no menu navigation code calls NotifyAuthenticationStateChanged. Successful authorized navigation needs none of those changes to explain this defect.

An unhandled permission-check exception could activate the outer boundary and remove its descendants. That is a separate error path, unlike a successful navigation followed by an otherwise healthy collapsed menu.

## 8. Root-cause analysis and confidence

| Explanation | Assessment |
|---|---|
| RouteData key replaces layout/menu | Confirmed deterministic cause in current source; framework semantics and RouteData identity verified |
| Responsive drawer closes on mobile | Verified additional behavior; cannot explain wide-desktop group reset or same-instance group collapse |
| PermissionView resets the containing group | Rejected for ordinary successful checks: wrappers are below groups and preserve prior _allowed while awaiting |
| MudNavGroup intentionally collapses on child navigation | No such path in exact 9.11.0 sources |
| Exclusive group expansion | Not configured; menu default allows multiple expansions |
| Custom CSS hides expanded content on route selection | No matching rule found |
| Full document reload, new circuit or runtime auth change | Possible independent state-reset mechanisms; no evidence needed for the verified code defect, and no live trace available |

CSS review: nav-section-container controls flex/gap; custom-modern-nav controls padding, colors, hover/active styles and indentation. The broad transition rule may affect appearance but does not alter expansion state or mount components. Legacy scoped selectors target navbar/nav-item/nav-scrollable markup absent from this Mud layout. Dashboard-only mobile CSS does not control drawer expansion.

**Limit:** this investigation proves the checkout's structural reset mechanism, not that a particular deployed browser was running this exact revision. If deployment differs, verify its Routes markup and trace reload/circuit events before transferring the conclusion.

## 9. Recommended fix — described only

For a future authorized implementation:

1. Remove the route-dependent identity key from the boundary that wraps AuthorizeRouteView. Keep the same boundary and shell instances for successful same-layout navigation.
2. Preserve authorization denial handling and exception protection. Give the boundary navigation context as an ordinary parameter, or use a correctly managed location-change notification.
3. On an actual destination change, explicitly recover a faulted boundary. Do not recover indiscriminately during rendering or repeatedly retry the failing page.
4. Leave NavMenu, its normal group state and the current Responsive/Md drawer unchanged initially.
5. Verify that the boundary redirects to access-denied and a permitted destination can render afterward.

Simply deleting the key without replacing its error-reset purpose is incomplete: the present PermissionErrorBoundary has no recovery mechanism and inherited error state could persist. Official guidance supports controlled Recover calls on navigation and warns against render-driven retry loops. [Blazor error-boundary recovery](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-9.0#error-boundaries).

If page-only error isolation is desired, a boundary scoped around the body below MainLayout is another valid design, but changes what errors it catches. That broader placement decision should not be hidden inside a menu-state patch. No patch or proposed code was applied here.

## 10. Alternative solutions

| Option | Evaluation |
|---|---|
| Normal MudNavGroup internal state after fixing shell identity | Preferred smallest approach for preserving manual expansion |
| Per-group @bind-Expanded | Useful for deliberate controlled behavior; cannot repair recreated owners |
| Change breakpoint or use Persistent drawer | Changes responsive UX and leaves the route-key defect intact; Md already supports the target laptop |
| Auto-expand by active descendant/current route | Separate direct-entry/refresh enhancement; may override manual collapse or multi-group choices |
| Small reusable navigation-group component | Only justified by a chosen active-descendant policy; a locally stateful wrapper is recreated too |
| Circuit-scoped navigation state | Can outlive layout components; compensates for the defect with extra state/service coupling |
| Browser/session storage | Supports reload persistence but adds lifecycle/storage behavior beyond this problem |
| Correct PermissionView/cache permissions | Could reduce repeated reads or address real permission flicker; not the proven reset cause |
| Key boundary by PageType or URI instead of RouteData | Still replaces layout on child-page changes; weaker workaround |

## 11. Is route-specific code necessary?

**No route list is needed for this fix.** Preserving the existing shell lets the library retain expansion without knowing which routes belong to which group.

Automatically revealing the active parent on a refresh/direct nested URL is a separate requirement. If chosen later, a group that learns its existing child links or a single navigation-definition source could avoid duplicated manual route tables. This investigation does not recommend introducing that abstraction now.

## 12. Risks and regressions for a future change

- Recovering too early or on every render can create exception/redirect loops.
- Removing the key without recovery can leave a faulted boundary stuck.
- Actual boundary errors may still remove its descendants; preserving state through errors is separate from successful-navigation preservation.
- Retaining layout may expose assumptions previously masked by reinitializing providers or local state.
- Same page type with changed route parameters/query string must still update correctly.
- Login/logout, permission denial, roles and StandaloneLayout transitions must keep their current security behavior.
- Mobile overlay closing, app-bar toggle, RTL placement and breakpoint transitions must remain usable.
- Navigation event subscriptions, if added, must be disposed; no subscriptions are required in NavMenu for the preferred preservation fix.

## 13. Suggested focused verification

These are future checks, not tests added or executed in this investigation.

1. At 1366×768 and 1920×1080, expand contacts, navigate suppliers → deliverymen → carriers, then cross to products. Check both selected-link styling and retained expansion.
2. Open two groups, navigate a child and confirm both remain expanded. Manually collapse one and ensure navigation does not reopen it without a chosen auto-expansion policy.
3. Use browser back/forward, query changes and same-page-type parameter changes. Confirm page data updates while the shell survives.
4. At 390×844, select a destination; confirm overlay closes. Reopen and verify the previously expanded group remains expanded.
5. Resize across 959/960 CSS pixels; verify drawer behavior separately from group state. Record window.innerWidth rather than only screen resolution.
6. In a future component integration test, render the actual routing composition and assert reference identity of MainLayout/NavMenu across successful same-layout navigation. A fresh render per page cannot test preservation.
7. Use a debugger or renderer component IDs to confirm group/state survival without permanent lifecycle logging.
8. Exercise a genuine AuthorizationDeniedException, then navigate to an allowed page. Verify recovery, no loop, no unauthorized content and expected error reporting.
9. Recheck permission-filtered links during slow permission responses and role changes, without assuming links and group headers share ownership.
10. Test refresh/direct /suppliers entry separately. Its parent may start collapsed under the current policy; that is not failure of normal navigation preservation.

## 14. Git and no-change verification

The initial tracked/untracked working tree was clean. It remained clean through file inspection, source research, reflection checks and the failed local-browser attempt. No source, Razor, CSS, configuration, project, test, solution, migration or package file was edited. No DI registration was changed. No formatter, app launch, source build/test run or repository debugging code was used.

No stage, commit, push, reset, rebase or branch mutation was performed. HEAD and branch remain as stated above. Final verification uses tracked diff and staged diff checks plus status inspection.

**The only working-tree addition is this explicitly requested Markdown report.** Thus the source working tree is unchanged; the complete tree is unchanged except for the report. This distinction avoids claiming a literally empty status after fulfilling the report requirement.

