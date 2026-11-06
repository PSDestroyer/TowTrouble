# First ride choice and body progression — 2026-11-06

After the first preferences screen, the player chooses one of four starter models. Browsing grants nothing. Confirmation grants exactly the chosen vehicle for free, saves it as the player's Stock vehicle, and opens Main. Host and Join reject an unchosen profile and return to the choice screen. A returning player receives no second gift. Keyboard, Xbox/PlayStation/Nintendo semantic Confirm/Back work through Title, first preferences, choice and review; sliders also support directional adjustment.

| Starter | Mass kg | Base pull kN | Top speed km/h |
| --- | ---: | ---: | ---: |
| Scout | 930 | 8.4 | 60 |
| Hauler | 960 | 8.8 | 56 |
| Buddy | 900 | 8.0 | 64 |
| Ranger | 950 | 8.6 | 58 |

Differences are deliberately small: Hauler offers a little extra pull/anchoring, Buddy more travel speed/agility, Scout and Ranger the middle ground. Only one is included. Other starter variants are optional alternatives at1200CR each, including Scout if another starter was chosen. Buying an alternative preserves the chosen starter identity and separate saved builds; it is not another free choice.

Every purchasable workshop body has more base pulling force than every starter. The first body in price order, Midjet Wood, costs1400CR and offers980kg /10kN /74km/h, improving all three headline values over every starter. With identical engine/transmission upgrades, it still has more pull. Higher heavy bodies retain their existing speed/mass tradeoffs. The overall powered speed ceiling remains80km/h. Garage Stock labels name the active platform, e.g. Ranger Stock.

Player stock stats are defined separately from recovery target archetypes. RecoveryVehicle.Masses/PullForces, authored target masses/soil loads, mission tiers/poses and map geometry remain unchanged. Purchased body mass/drive/speed continue to override starter values. Existing equipped engines, transmissions, tires and inventory are retained.

Profile v4 stores starterChosen and starterVehicle, alongside vehicle ownership. Fresh profiles own nothing until confirmation; confirmation sets one ownership bit without spending credits. Unused old implicit-Scout profiles receive the new choice. Existing profiles with progress, money, invested equipment, saved setups or another selected/owned model retain their ownership and active ride; no reset or new gift is applied. Earlier v1 credit/paint and v3 ownership migrations remain compatible. Other-model purchases do not change starterVehicle. The existing PlayerPrefs key/backups are retained.

Current economy and the six-job introduction are defined in `CareerEconomy.md` (supersedes the earlier independent cable prices/reward budgets). Cables are11kN included,14kN200CR,18kN1200CR,26kN2000CR,36kN4200CR. First completion adds25%; replay fees scale by difficulty. Offers include route length/ascent and estimated handling time; elapsed idle time never increases the reward. The first two opening jobs guarantee2375CR combined on their first completion even without performance bonuses, enough for the1400CR body. Validated six-job paths exist for every starter with either body-first or engine/tire-first buying. Inventory, target physics and stable completion IDs are preserved.
