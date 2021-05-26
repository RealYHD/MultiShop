import os
import shutil


SERVER_DIR = "src/MultiShop/Server"
DATA_DIR = "Data"
DB_MIGRATE_CMD = "dotnet ef migrations add InitialCreate -o {0}"
DB_UPDATE_CMD = "dotnet ef database update"

os.chdir(os.path.dirname(os.path.realpath(__file__)))
os.chdir("..")
os.chdir(SERVER_DIR)
print("Working in: " + os.getcwd())

migrationsDir = os.path.join(DATA_DIR, "Migrations")

print("Deleting current migrations directory if it exists.")
shutil.rmtree(migrationsDir, ignore_errors=True)

print("Deleting old app.db if it exists.")
if os.path.exists("app.db"):
    os.remove("app.db")

print("Creating migration.")
os.system(DB_MIGRATE_CMD.format(migrationsDir))

print("Updating database.")
os.system(DB_UPDATE_CMD)