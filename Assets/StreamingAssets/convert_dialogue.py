import json
import re

def parse_narrative_to_json(input_file, output_file):
    """
    Convert narrative dialogue format to structured JSON format.
    """
    
    with open(input_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Split content by dialogue sections
    sections = re.split(r'\[Dialogue \d+.*?\]', content)
    section_titles = re.findall(r'\[Dialogue \d+.*?\]', content)
    
    # Also get transition sections
    transitions = re.findall(r'\[Transition\]', content)
    
    conversations = []
    
    for i, section in enumerate(sections[1:], 1):  # Skip first empty section
        lines = section.strip().split('\n')
        dialogues = []
        
        for line in lines:
            line = line.strip()
            if not line:
                continue
            
            # Match pattern [CharacterName] - DialogueText
            match = re.match(r'\[([^\]]+)\]\s*[-–]\s*(.+)', line)
            if match:
                character = match.group(1)
                text = match.group(2)
                
                # Map character names
                char_map = {
                    'Lily': 'Player2',
                    'Scepter': 'Player1',
                    'Leon': 'Player1',  # Leon is revealed to be Player1
                    'Scene': 'narrator',
                    'Nexus': 'Nexus',
                    'Blacksmith': 'blacksmith',
                    'Priest': 'priest',
                    'Farmer': 'farmer',
                    'Woman': 'Woman',
                    'Anna': 'Anna',
                    'Cleric': 'Cleric',
                    'Revolutionary Member': 'enemy1',
                    'Revolutionary Leader': 'enemy2',
                    'Elysium': 'elysium',
                    'Commander Angus': 'CommanderAngus',
                    'Gameplay': 'Gameplay',
                    'Transition': 'narrator'
                }
                
                mapped_char = char_map.get(character, character)
                
                dialogues.append({
                    "CharacterName": mapped_char,
                    "DialogueText": text
                })
        
        # Generate conversation key from section title
        if i - 1 < len(section_titles):
            title = section_titles[i - 1]
            # Extract a meaningful key from the title
            key_match = re.search(r'\[Dialogue \d+ - (.+?)\]', title)
            if key_match:
                key_name = key_match.group(1).lower()
                key_name = re.sub(r'[^\w\s]', '', key_name)
                key_name = key_name.replace(' ', '_')
            else:
                key_name = f"dialogue_{i}"
        else:
            key_name = f"dialogue_{i}"
        
        if dialogues:
            conversations.append({
                "conversationKey": key_name,
                "Dialogues": dialogues
            })
    
    # Create final structure
    output_data = {
        "conversations": conversations
    }
    
    # Write to output file with custom formatting (CharacterName and DialogueText on same line)
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write('{\n  "conversations": [\n')
        for i, conv in enumerate(conversations):
            f.write('    {\n')
            f.write(f'      "conversationKey": "{conv["conversationKey"]}",\n')
            f.write('      "Dialogues": [\n')
            for j, dialogue in enumerate(conv["Dialogues"]):
                comma = "," if j < len(conv["Dialogues"]) - 1 else ""
                # Escape quotes in dialogue text
                char_name = dialogue["CharacterName"].replace('"', '\\"')
                dialogue_text = dialogue["DialogueText"].replace('"', '\\"')
                f.write(f'        {{ "CharacterName": "{char_name}", "DialogueText": "{dialogue_text}" }}{comma}\n')
            comma = "," if i < len(conversations) - 1 else ""
            f.write(f'      ]\n')
            f.write(f'    }}{comma}\n')
        f.write('  ]\n')
        f.write('}\n')
    
    print(f"Conversion complete! Output written to: {output_file}")
    print(f"Total conversations: {len(conversations)}")
    for conv in conversations:
        print(f"  - {conv['conversationKey']}: {len(conv['Dialogues'])} dialogues")

if __name__ == "__main__":
    input_file = "tutorial_conversation_new.json"
    output_file = "tutorial_conversation_new_converted.json"
    
    try:
        parse_narrative_to_json(input_file, output_file)
    except FileNotFoundError:
        print(f"Error: Could not find {input_file}")
    except Exception as e:
        print(f"Error during conversion: {e}")
