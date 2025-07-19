#!/usr/bin/env python3
"""
Script to generate synthetic test PDFs for document processing application.
Creates 15 recipe PDFs and 15 poem PDFs with consistent type identifiers and variable fields.
"""

import os
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.units import inch
import random

def create_recipe_pdf(filename, recipe_data):
    """Create a recipe PDF with consistent type identifiers and variable fields."""
    c = canvas.Canvas(filename, pagesize=letter)
    width, height = letter
    
    # Consistent type identifier for recipes
    c.setFont("Helvetica-Bold", 16)
    c.drawString(100, height - 100, "RECIPE COLLECTION")
    c.drawString(100, height - 120, "Traditional Cooking Methods")
    
    # Recipe title
    c.setFont("Helvetica-Bold", 14)
    c.drawString(100, height - 160, f"Recipe: {recipe_data['name']}")
    
    # Variable fields with consistent labels
    c.setFont("Helvetica", 12)
    y_pos = height - 200
    
    c.drawString(100, y_pos, f"Region: {recipe_data['region']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Cooking Time: {recipe_data['time']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Difficulty: {recipe_data['difficulty']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Serves: {recipe_data['serves']}")
    y_pos -= 40
    
    # Consistent section headers
    c.setFont("Helvetica-Bold", 12)
    c.drawString(100, y_pos, "INGREDIENTS:")
    y_pos -= 20
    
    c.setFont("Helvetica", 10)
    for ingredient in recipe_data['ingredients']:
        c.drawString(120, y_pos, f"• {ingredient}")
        y_pos -= 15
    
    y_pos -= 20
    c.setFont("Helvetica-Bold", 12)
    c.drawString(100, y_pos, "INSTRUCTIONS:")
    y_pos -= 20
    
    c.setFont("Helvetica", 10)
    for i, instruction in enumerate(recipe_data['instructions'], 1):
        c.drawString(120, y_pos, f"{i}. {instruction}")
        y_pos -= 15
    
    c.save()

def create_poem_pdf(filename, poem_data):
    """Create a poem PDF with consistent type identifiers and variable fields."""
    c = canvas.Canvas(filename, pagesize=letter)
    width, height = letter
    
    # Consistent type identifier for poems
    c.setFont("Helvetica-Bold", 16)
    c.drawString(100, height - 100, "POETRY ANTHOLOGY")
    c.drawString(100, height - 120, "Literary Works Collection")
    
    # Poem title
    c.setFont("Helvetica-Bold", 14)
    c.drawString(100, height - 160, f"Title: {poem_data['title']}")
    
    # Variable fields with consistent labels
    c.setFont("Helvetica", 12)
    y_pos = height - 200
    
    c.drawString(100, y_pos, f"Author: {poem_data['author']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Era: {poem_data['era']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Style: {poem_data['style']}")
    y_pos -= 20
    c.drawString(100, y_pos, f"Theme: {poem_data['theme']}")
    y_pos -= 40
    
    # Consistent section header
    c.setFont("Helvetica-Bold", 12)
    c.drawString(100, y_pos, "POEM TEXT:")
    y_pos -= 30
    
    c.setFont("Helvetica", 11)
    for line in poem_data['lines']:
        c.drawString(120, y_pos, line)
        y_pos -= 20
    
    c.save()

def generate_recipe_data():
    """Generate varied recipe data for testing."""
    recipes = [
        {
            'name': 'Beef Goulash',
            'region': 'Eastern Europe',
            'time': '2 hours',
            'difficulty': 'Medium',
            'serves': '4-6',
            'ingredients': ['2 lbs beef chuck', '2 onions', '3 tbsp paprika', '2 cups beef broth'],
            'instructions': ['Brown the beef in oil', 'Add onions and cook until soft', 'Add paprika and broth', 'Simmer for 1.5 hours']
        },
        {
            'name': 'Pasta Carbonara',
            'region': 'Southern Italy',
            'time': '30 minutes',
            'difficulty': 'Easy',
            'serves': '2-3',
            'ingredients': ['8 oz pasta', '4 eggs', '1 cup parmesan', '4 oz pancetta'],
            'instructions': ['Cook pasta al dente', 'Whisk eggs with cheese', 'Cook pancetta until crispy', 'Toss hot pasta with egg mixture']
        },
        {
            'name': 'Pad Thai',
            'region': 'Southeast Asia',
            'time': '45 minutes',
            'difficulty': 'Medium',
            'serves': '3-4',
            'ingredients': ['8 oz rice noodles', '2 tbsp tamarind paste', '3 tbsp fish sauce', '2 eggs'],
            'instructions': ['Soak noodles in warm water', 'Heat wok with oil', 'Scramble eggs', 'Add noodles and sauce']
        },
        {
            'name': 'Chicken Tikka Masala',
            'region': 'Northern India',
            'time': '1 hour',
            'difficulty': 'Hard',
            'serves': '4-5',
            'ingredients': ['2 lbs chicken breast', '1 cup yogurt', '2 tbsp garam masala', '1 can tomatoes'],
            'instructions': ['Marinate chicken in yogurt', 'Grill chicken pieces', 'Make tomato sauce', 'Combine chicken with sauce']
        },
        {
            'name': 'Fish Tacos',
            'region': 'Western Mexico',
            'time': '25 minutes',
            'difficulty': 'Easy',
            'serves': '2-4',
            'ingredients': ['1 lb white fish', '8 corn tortillas', '1 cup cabbage', '1 lime'],
            'instructions': ['Season and grill fish', 'Warm tortillas', 'Shred cabbage', 'Assemble tacos with lime']
        }
    ]
    
    # Generate variations
    regions = ['Eastern Europe', 'Southern Italy', 'Southeast Asia', 'Northern India', 'Western Mexico', 
               'Southern France', 'Northern China', 'Eastern Europe', 'Southern Italy', 'Western Mexico']
    difficulties = ['Easy', 'Medium', 'Hard']
    times = ['15 minutes', '30 minutes', '45 minutes', '1 hour', '1.5 hours', '2 hours']
    
    all_recipes = []
    for i in range(15):
        if i < len(recipes):
            recipe = recipes[i].copy()
        else:
            # Generate variations of existing recipes
            base_recipe = recipes[i % len(recipes)].copy()
            base_recipe['name'] = f"{base_recipe['name']} Variation {i-4}"
            base_recipe['region'] = random.choice(regions)
            base_recipe['difficulty'] = random.choice(difficulties)
            base_recipe['time'] = random.choice(times)
            recipe = base_recipe
        
        all_recipes.append(recipe)
    
    return all_recipes

def generate_poem_data():
    """Generate varied poem data for testing."""
    poems = [
        {
            'title': 'Morning Dewdrops',
            'author': 'Sarah Johnson',
            'era': 'Contemporary',
            'style': 'Free Verse',
            'theme': 'Nature',
            'lines': ['The morning dew glistens bright', 'On petals soft and white', 'A gentle breeze whispers low', 'As flowers dance to and fro']
        },
        {
            'title': 'City Lights',
            'author': 'Michael Chen',
            'era': 'Modern',
            'style': 'Narrative',
            'theme': 'Urban Life',
            'lines': ['Neon signs flicker and glow', 'In the city far below', 'People hurry through the night', 'Searching for that perfect light']
        },
        {
            'title': 'Ocean Waves',
            'author': 'Emma Rodriguez',
            'era': 'Contemporary',
            'style': 'Lyrical',
            'theme': 'Seascape',
            'lines': ['Waves crash upon the shore', 'With sounds like never before', 'The ocean vast and deep', 'Holds secrets it will keep']
        },
        {
            'title': 'Autumn Leaves',
            'author': 'David Kim',
            'era': 'Classical',
            'style': 'Sonnet',
            'theme': 'Seasons',
            'lines': ['Golden leaves fall from the tree', 'Dancing in the autumn breeze', 'Nature paints with colors bold', 'Stories that will never grow old']
        },
        {
            'title': 'Mountain Peak',
            'author': 'Lisa Thompson',
            'era': 'Romantic',
            'style': 'Ballad',
            'theme': 'Adventure',
            'lines': ['High above the clouds so white', 'Stands a peak of mighty height', 'Reaching for the endless sky', 'Where eagles soar and spirits fly']
        }
    ]
    
    # Generate variations
    eras = ['Contemporary', 'Modern', 'Classical', 'Romantic', 'Victorian', 'Medieval']
    styles = ['Free Verse', 'Narrative', 'Lyrical', 'Sonnet', 'Ballad', 'Haiku']
    themes = ['Nature', 'Urban Life', 'Seascape', 'Seasons', 'Adventure', 'Love', 'Loss', 'Hope']
    authors = ['Sarah Johnson', 'Michael Chen', 'Emma Rodriguez', 'David Kim', 'Lisa Thompson',
               'Robert Wilson', 'Maria Garcia', 'James Anderson', 'Jennifer Lee', 'Thomas Brown']
    
    all_poems = []
    for i in range(15):
        if i < len(poems):
            poem = poems[i].copy()
        else:
            # Generate variations of existing poems
            base_poem = poems[i % len(poems)].copy()
            base_poem['title'] = f"{base_poem['title']} Part {i-4}"
            base_poem['author'] = random.choice(authors)
            base_poem['era'] = random.choice(eras)
            base_poem['style'] = random.choice(styles)
            base_poem['theme'] = random.choice(themes)
            poem = base_poem
        
        all_poems.append(poem)
    
    return all_poems

def main():
    """Main function to generate all test PDFs."""
    # Create directories
    os.makedirs('recipes', exist_ok=True)
    os.makedirs('poems', exist_ok=True)
    
    # Generate recipe PDFs
    print("Generating recipe PDFs...")
    recipe_data = generate_recipe_data()
    for i, recipe in enumerate(recipe_data, 1):
        filename = f"recipes/recipe_{i:02d}_{recipe['name'].replace(' ', '_').lower()}.pdf"
        create_recipe_pdf(filename, recipe)
        print(f"Created: {filename}")
    
    # Generate poem PDFs
    print("\nGenerating poem PDFs...")
    poem_data = generate_poem_data()
    for i, poem in enumerate(poem_data, 1):
        filename = f"poems/poem_{i:02d}_{poem['title'].replace(' ', '_').lower()}.pdf"
        create_poem_pdf(filename, poem)
        print(f"Created: {filename}")
    
    print(f"\nSuccessfully generated {len(recipe_data)} recipe PDFs and {len(poem_data)} poem PDFs!")

if __name__ == "__main__":
    main()