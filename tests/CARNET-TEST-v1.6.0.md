# Carnet de Test — v1.6.0

## 🔧 Feature 3 — Windrose+ Toggle On/Off

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Toggle OFF préserve les fichiers W+ | Activer W+, aller dans Settings → décocher "Windrose+" | Toast "Windrose+ désactivé", fichiers .wplus-version et windrose_plus/ toujours présents | ✅ |
| 2 | Toggle ON avec fichiers manquants | Désinstaller W+, cocher "Windrose+" | Toast + ouverture du dialog d'installation | ✅ |
| 3 | Toggle ON avec fichiers présents | Avoir W+ installé, décocher puis recocher | Toast "Windrose+ activé", pas de dialog | ✅ |
| 4 | Avertissement redémarrage | Serveur en cours d'exécution, toggler OFF ou ON | Toast "Redémarrage requis" | ✅ |
| 5 | Persistance après redémarrage de l'app | Toggler OFF, fermer/rouvrir l'app | Toggle toujours OFF | ✅ |

## 🔧 Feature 4 — Version Pinning

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Dropdown liste les versions | Aller dans Settings → section Windrose+ | ComboBox avec "(Latest)" + toutes les releases GitHub | ✅ |
| 2 | "(Latest)" = pas de pin | Sélectionner une version, puis "(Latest)" | `PinnedWindrosePlusVersion` = null, mise à jour automatique | ✅ |
| 3 | Version épinglée persistée | Choisir v1.5.3, fermer/rouvrir l'app | Dropdown toujours sur v1.5.3 | ✅ |
| 4 | Indicateur de chargement | Ouvrir Settings avec réseau lent | ProgressBar visible pendant le chargement | ✅ |
| 5 | Version installée affichée | W+ installé, version épinglée quelconque | Texte "Installée: vX.Y.Z" sous le dropdown | ✅ |

## 💾 Feature 5 — Per-Server Backup & Mods Folders

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | BackupDirOverride null par défaut | Créer nouveau serveur | Backup utilise le dossier global | ✅ |
| 2 | ModsDirOverride null par défaut | Créer nouveau serveur | Mods utilise le dossier par défaut | ✅ |
| 3 | Browse dossier backup | Settings → section Per-Server → Browse | FilePicker s'ouvre, chemin appliqué, toast confirmé | ✅ |
| 4 | Browse dossier mods | Settings → section Per-Server → Browse (mods) | FilePicker s'ouvre, chemin appliqué | ✅ |
| 5 | Reset backup dir | Cliquer "Reset" à côté du champ backup | Champ vidé, retour au dossier global | ✅ |
| 6 | Reset mods dir | Cliquer "Reset" à côté du champ mods | Champ vidé, retour au dossier par défaut | ✅ |
| 7 | Backup utilise l'override | Configurer un override, lancer un backup | Fichiers créés dans le dossier override | ✅ |
| 8 | Mods utilise l'override | Configurer un override, installer un mod | Mods installés dans le dossier override | ✅ |
| 9 | Persistance des overrides | Définir les dossiers, fermer/rouvrir l'app | Chemins toujours présents | ✅ |
| 10 | Surcharge JSON rétrocompatible | Ouvrir settings.json existant sans overrides | `BackupDirOverride` = null, `ModsDirOverride` = null | ✅ |

## 🖥️ Feature 6 — System Tray Improvements

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | CloseToTray désactivé par défaut | Aller dans Settings | Checkbox décochée | ✅ |
| 2 | CloseToTray = OFF → fermeture normale | Décocher, cliquer X | App se ferme complètement | ✅ |
| 3 | CloseToTray = ON → fermeture dans tray | Cocher, cliquer X | Fenêtre disparaît, icône tray reste, processus en vie | ✅ |
| 4 | Tray "Show" restaure la fenêtre | Fenêtre cachée, clic droit tray → "Show" | Fenêtre réapparaît, focus | ✅ |
| 5 | Tray "Quit" ferme tout | Fenêtre ouverte ou cachée, tray → "Quit" | Processus terminé | ✅ |
| 6 | --tray démarre caché | Lancer avec `--tray` | Pas de fenêtre, icône tray seule | ✅ |
| 7 | --minimized = --tray | Lancer avec `--minimized` | Même comportement que --tray | ✅ |
| 8 | Fenêtre native X interceptée | CloseToTray = ON, cliquer X dans la barre Windows | Fenêtre cachée (pas fermée) | ✅ |
| 9 | Persistance du paramètre | Cocher CloseToTray, fermer/rouvrir | Checkbox toujours cochée | ✅ |

## ⚠️ Feature 8 — Mod Conflict Scanner

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Scanner s'exécute au chargement des mods | Ouvrir la page Mods | Tous les mods scannés, conflits détectés | ✅ |
| 2 | Icône conflit sur carte de mod | Installer un mod conflictuel (ex: MoreStacks) | Icône ambrée + texte "Conflit détecté" | ✅ |
| 3 | Bannière dans l'éditeur QoL | Ouvrir Editor avec multiplicateurs en conflit | Bannière rouge avec liste des conflits actifs | ✅ |
| 4 | Scan pré-lancement du serveur | Démarrer le serveur avec des mods conflictuels | Log des conflits, événement ConflictsDetected | ✅ |
| 5 | Aucun faux positif | Mods sans conflit connus | Pas d'alerte | ✅ |
| 6 | Description du conflit lisible | Hover ou lire le texte de l'alerte | Texte expliquant quel paramètre est affecté | ✅ |

## 📐 Feature 9 — QoL Editor (Windrose+ Multipliers)

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Page éditeur accessible | Navigation → "INI Editor" | Page Editor avec catégories et sliders | ✅ |
| 2 | Catégories affichées | W+ actif | Economy, Farming, Inventory, Character | ✅ |
| 3 | Slider XP | Glisser le slider XP | Valeur mises à jour en temps réel | ✅ |
| 4 | Slider Loot | Glisser le slider Loot | Valeur mises à jour | ✅ |
| 5 | Slider Stack Size | Glisser le slider Stack Size | Valeur mise à jour, avertissement "zone dangereuse" | ✅ |
| 6 | Sauvegarde | Cliquer "Save" | Valeurs persistées dans windrose_plus.json, toast "Saved" | ✅ |
| 7 | Reset | Cliquer "Reset All" | Tous les sliders reviennent à 1.0 | ✅ |
| 8 | État vide (W+ désactivé) | W+ désactivé pour ce serveur | Message "Opt out" avec explication | ✅ |
| 9 | Redémarrage requis | Modifier et sauvegarder | Message "Restart Required" | ✅ |
| 10 | Persistance | Modifier, sauvegarder, fermer/rouvrir | Valeurs conservées | ✅ |

## ↔️ Feature 10 — Fenêtre Redimensionnable

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Redimensionnement | Attraper le bord de la fenêtre et glisser | Fenêtre se redimensionne | ✅ |
| 2 | Taille minimum | Réduire en dessous de 900×600 | Bloqué à 900×600 | ✅ |
| 3 | Taille par défaut | Lancer l'app | 1440×960 | ✅ |
| 4 | Dialog non redimensionnable | Ouvrir un dialog (About, Confirm, etc.) | Taille fixe, pas de resize | ✅ |
| 5 | Contenu responsive | Redimensionner largeur | Contenu s'adapte (ScrollViewer) | ✅ |

## ⏱️ Feature 11 — Auto-Start Delay

| # | Cas | Étapes | Résultat Attendu | Statut |
|---|-----|--------|------------------|--------|
| 1 | Délai à 0 par défaut | Aller dans Settings | NumericUpDown à 0 | ✅ |
| 2 | Délai configurable | Monter à 15s | Enregistré, délai appliqué au prochain démarrage | ✅ |
| 3 | Limite 60s max | Essayer de mettre 120 | Bloqué à 60 | ✅ |
| 4 | Persistance | Définir 30s, fermer/rouvrir | Toujours 30s | ✅ |

---

**Résumé : 42 cas de test — 42 attendus ✅ — 0 échec**
