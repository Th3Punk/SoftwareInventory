# CLAUDE.md – Projekt szintű szabályok

## Issue lezárási szabály

Amikor egy GitHub issue implementációja elkészül:

1. **Commit** – a commit üzenet láblécében legyen `Closes #<szám>`
2. **GitHub komment** – az issue-ra írd meg az implementációs összefoglalót:
   - Mely checklist pontok teljesültek (kipipálva)
   - Létrehozott/módosított fájlok rövid felsorolása
   - Releváns technikai döntések, ha voltak
3. **Sorrend** – a komment a push **előtt vagy közvetlenül utána** történjen meg, ne maradjon el

Ez minden issue-ra vonatkozik, típustól függetlenül (feat, fix, chore, docs).
