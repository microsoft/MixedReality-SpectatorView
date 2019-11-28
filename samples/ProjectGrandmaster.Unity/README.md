# Project Grandmaster

Project Grandmaster was designed to explore the possibilities of how Mixed Reality can be used to facilitate a fun experience of playing chess without the unnatural interaction restrictions of moving chess pieces, whilst also enforcing the rules.

This game allows players to have full control over the manipulation of 3D chess pieces using hand interaction, however, to improve the game feel and further immerse the players, a set of animations have been put into place. These animations range from knocking down of pieces as part of the forfeit mechanism to moving back pieces to their last valid positions when "accidentally" knocked over by collision with another chess piece.

The game has been designed for the emerging HoloLens 2, however, with the aid of far interaction and speech recognition, it can be played on the HoloLens 1, with limited functionalities.

## How to play

To take full advantage of the HoloLens 2's capabilities there are a set of different ways the game can be played.

### Eye Gaze

### Speech recognition

### Gesture

### Grabbable and touchable

## Player assistance features

### Piece and tile highlight

### On-board menu

## Additional features

### Ghosting System

### Piece fix mechanism

## Prepare your local codebase

1. Run `tools/Scripts/SetupRepository.bat` (`tools/Scripts/SetupRepository.sh` on Mac) to setup your local version of the code base. This script will obtain the external dependencies required for building this project. It is required that you are using MRTK-Unity version 2.0+
2. Run `FixSymbolicLinks.bat` (`FixSymbolicLinks.sh` on Mac) to fix all links to the external dependencies.

## About the team

Project Grandmaster was created as part of a capstone project by a team of University of Sydney students in collaboration with the engineers at Microsoft.

USYD Team:
1. Hrithvik (Jacob) Sood
2. John Tran
3. Tom Derrick
4. Aydin Ucan
5. Aayush Jindal

Client Representatives:
1. Andrei Borodin
2. Will Wei

Supervisor:
1. Julian Mestre
