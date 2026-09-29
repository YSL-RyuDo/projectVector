# Project Vector editing constraints

- Preserve existing gameplay, profile, honor, store sections and serialized button references when changing an unrelated feature. Do not replace whole screens or blanket-disable existing children to mount a new UI.
- Author UI as saved scene/prefab objects, visible and editable in Unity Hierarchy before Play. Do not create the shop UI in Awake/Start/OnEnable or rebuild it every launch. Runtime instances are appropriate for temporary 3D preview models/effects, not a replacement for authored screen layout.
- Shop visual target is the user's supplied reference screenshots: grouped rewards on the left, rotatable 3D preview in the center, description on the right; retain the project's existing UI assets and styling.
- The screenshot target applies to the entire shop, not only reward details: recommendation banners, shop tabs, category filters with a four-column catalog, and a large draw preview with right-hand draw/actions. Omit cash charging, paid memberships, VIP and real-money pricing; do not invent nonfunctional event tabs.
- Keep shop ownership separate from the all-unlocked gameplay test inventory. Currency 0/1 must use the existing CareerWallet and MoneyManager, never a second shop wallet. Only the exact qwer account may receive test currency. No real-money payment/charging feature. Keep internal validation/test/debug explanations out of player-facing UI.
- Do not use computer-use/UI automation, close the user's Unity editor, or overwrite unsaved scenes. Use code/file and explicit editor batch methods only when the project is not open in another Unity process.
- Keep diagnostic output/builds outside the repository. Never delete account data as build cleanup.
