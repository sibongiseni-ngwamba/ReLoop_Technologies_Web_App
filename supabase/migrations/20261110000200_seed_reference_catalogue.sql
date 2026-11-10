insert into public.reward_items (reward_item_id, item_name, description, points_cost, stock_quantity)
values
  ('item_001', '$5 Coffee Shop Voucher', 'Valid at participating organic coffee outlets nationwide.', 500, 50),
  ('item_002', '10% Off Eco Grocery', 'Discount code for sustainable, organic food products.', 800, 35),
  ('item_003', 'Plant a Tree in Your Name', 'We will plant a native tree to offset your carbon footprint.', 1000, 100),
  ('item_004', '$15 Eco Store Gift Card', 'Redeemable online at EcoMart sustainable store.', 1500, 20),
  ('item_005', 'Solar Powerbank 10,000mAh', 'Portable eco-friendly solar-powered phone charger.', 2500, 15),
  ('item_006', 'Stainless Steel Water Bottle', 'Insulated 750ml reusable metal flask.', 1200, 40),
  ('item_007', 'Reusable Bamboo Cutlery Set', 'Travel pouch with bamboo fork, spoon, knife, and straw.', 600, 60),
  ('item_008', 'Organic Cotton Tote Bag', 'Durable, washable zero-waste shopping bag.', 400, 75),
  ('item_009', 'Home Compost Bin Kit 15L', 'Odor-free indoor bokashi composting starter bucket.', 3000, 10),
  ('item_010', 'Electric Scooter 30-Min Pass', 'Free riding credit on eco electric city scooters.', 700, 50)
on conflict (reward_item_id) do nothing;

insert into public.waste_categories (waste_category_id, category_name, description, model_label)
values
  ('cat_001', 'Plastics', 'PET, HDPE, soda bottles, milk jugs, food tubs.', 'Plastic Bottle'),
  ('cat_002', 'Metals & Cans', 'Aluminum beverage cans, steel food tins, foil.', 'Aluminum Can'),
  ('cat_003', 'Paper & Cardboard', 'Newspapers, cereal boxes, corrugated cardboard.', 'Cardboard Box'),
  ('cat_004', 'Glass', 'Clear, green, and brown glass bottles and jars.', 'Glass Jar'),
  ('cat_005', 'E-Waste & Gadgets', 'Cell phones, chargers, laptops, circuit boards.', 'Electronic Gadget'),
  ('cat_006', 'Organic & Compost', 'Fruit peelings, vegetable remains, coffee grounds.', 'Organic Waste'),
  ('cat_007', 'Batteries', 'AA, AAA, 9V, lithium-ion cell phone batteries.', 'Battery'),
  ('cat_008', 'Textiles & Clothes', 'Wearable clothes, linens, fabrics, shoes.', 'Textile'),
  ('cat_009', 'Hazardous Waste', 'Paint cans, motor oil, household cleaning chemicals.', 'Hazardous Container'),
  ('cat_010', 'Rubber & Tyres', 'Bicycle and auto tyres, rubber mats.', 'Rubber Tyre')
on conflict (waste_category_id) do nothing;

insert into public.guidance_content (guidance_id, waste_category_id, title, content)
values
  ('guide_001', 'cat_001', 'How to Recycle Plastics Correctly', 'Look for resin codes #1 (PET) and #2 (HDPE). Rinse containers to remove food and liquid residue. Follow local guidance for caps.'),
  ('guide_002', 'cat_002', 'Metal & Aluminum Can Preparation', 'Rinse cans before recycling. Keep materials separate when local collection guidance requires it.'),
  ('guide_003', 'cat_003', 'Cardboard & Paper Sorting Tips', 'Flatten cardboard boxes. Keep paper dry and separate paper with food or grease from clean recyclables.'),
  ('guide_004', 'cat_004', 'Glass Bottles & Jars Safety', 'Separate bottles and jars as requested by your local recycler. Keep mirrors, ceramics, and window panes out of bottle-glass recycling.'),
  ('guide_005', 'cat_005', 'E-Waste & Electronics Recycling', 'Remove personal data from devices. Use an approved e-waste collection point for electronics and batteries.'),
  ('guide_006', 'cat_006', 'Home Composting 101', 'Balance food scraps with dry leaves or paper. Keep compost moist and aerate it regularly.'),
  ('guide_007', 'cat_007', 'Battery Handling & Disposal', 'Cover exposed battery terminals and store used batteries safely before taking them to a collection point.'),
  ('guide_008', 'cat_008', 'Textile & Clothing Circularity', 'Donate wearable items. Keep clean, dry textiles separate from general recyclables.')
on conflict (guidance_id) do nothing;
