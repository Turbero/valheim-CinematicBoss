### CHANGELOG

## 1.2.3

* Fixed cutscene blocking players at Deep North altar for fallen warriors (use /remove_cutscene if trapped in general).
* Added altar exclusion list names (not boss names) as option in .cfg to not trigger cutscene (typically "offeraltar_<something>" format). Example: offeraltar_deer to skip cutscene in Eikthyr
* If any missions active from Marketplace mod, it will also be hidden now during cutscene.

## 1.2.2

* Fix to not trigger cinematic in the last boss altar outside (go there and check out by yourself why, no spoilers!)

## 1.2.1

* Recompiled for compatibility with 1.0
* ServerSync upgrade

## 1.2.0

* Added configurable transparency effects for bosses with obstacles around when invoking them in their altars.
  * Configurable list of bosses to use this effect. Add their prefabNames in the .cfg file (Eikthyr,gd_king,Bonemass,Dragon,GoblinKing,SeekerQueen,Fader) and even other mods enemies.
  * By default, it will apply to Eikthyr and Elder (gd_king)
  * Other effects like alpha value and area of transparency effect available to manipulate in .cfg file as well

## 1.1.3

* Fixed issue where players could block, jump or roll with dodge action during the cutscene.
* Restricted "/remove_cutscene" command for admins only (this ends the player cutscene instantly)

## 1.1.2

* Cinematic skipped when boss is already out around the altar (to avoid getting stuck and destroyed by the first spawn)
* Decreased Eikthyr waiting time for camera to go back to player one second less (it was 3, now 2)

## 1.1.1

* Changed option "Camera waits at boss (seconds)" into "Camera waits at boss until he is fully out (true/false)". Now the waiting time matches for each vanilla boss exactly until they are fully out.

## 1.1.0

Added:
* support for multiplayer with options for players nearby to receive the boss cutscene
* server-sync capability
* new options to avoid calling a boss with enemies nearby. Set the detection range as well in the .cfg file
* new command /removecutscene to restore the camera back to the player if a game crash ever happens during the cutscene

## 1.0.0

Initial version