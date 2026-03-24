### CHANGELOG

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