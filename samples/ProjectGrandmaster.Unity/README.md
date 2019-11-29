# Project Grandmaster

Project Grandmaster was designed to explore the possibilities of how Mixed Reality can be used to facilitate a fun experience of playing chess without the unnatural interaction restrictions of moving chess pieces, whilst also enforcing the rules.

This game allows players to have full control over the manipulation of 3D chess pieces using hand interaction, however, to improve the game feel and further immerse the players, a set of animations have been put into place. These animations range from knocking down of pieces as part of the forfeit mechanism to moving back pieces to their last valid positions when "accidentally" knocked over by collision with another chess piece.

The game has been designed for the emerging HoloLens 2, however, with the aid of far interaction and speech recognition, it can be played on the HoloLens 1, with limited functionalities.

## How to play

To take full advantage of the HoloLens 2's capabilities there are a set of different ways the game can be played.

### Eye Gaze

The player is able to move a chess pieces through eye gaze and voice commands. By looking at the piece and saying "Select", the piece will float above its position, as a way of indicating the piece has been selected and is now waiting for the next step.

Once selected, the player can gaze on any potential tile for the piece and say "Move". This will start an animation leading the piece to the chosen location. If the selected tile is an invalid position, the piece will move back to its original spot, otherwise, it's the opponent's turn.  

### Speech Recognition

The game features various voice commands, including "Undo", "Reset", "Open Menu", "Close Menu", and for all other options available in the hand menu system, as displayed by its 'see it say it label'. 

### Gesture

The way the hand menu is brought up is by facing the left palm upwards and using the other hand for selection. 

### Grabbable and Touchable

The pieces can be manipulated using direct hand interactions and far interaction pointers. Along with this are the on-board menu buttons which can be pressed to undo the previous move or reset the game to the starting positions. 

## Player Assistance Features

In order to make the game self-intuitive and user friendly, we have implemented a piece and tile highlight system that for a selected piece it highlights all the valid positions it can move to. 

Along with this, there is an arrow on the on-board menu which changes colour between green and red. Green if it's the player's turn, and red if it's the opponent's go. 

## Additional Features

### Forfeit Mechanism

If the player chooses to forfeit, they can do so in 3 distinct ways.

1. Grab and move the king onto the dedicated tile on the on-board menu, labelled "Forfeit Tile". By doing this, player's pieces will all "fall down" and the game with enter sandbox mode.

2. Through the voice command, "Forfeit"

3. Flipping the board using the box handler (disabled by default. can be enabled through the 'chessboard' game object)

### Ghosting System

## Miscellaneous Features

### Surface Magnetism

Surface magnetism allows the game to aligned onto a real-world surface, giving it a more authentic feel. This can be enabled on the hand-menu under settings.

### Chess Customisation

The game can be changed between the classic black and white look to a more modern look - gold and black. Available under game settings on the hand-menu. 

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
