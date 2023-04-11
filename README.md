# Atea

## Task 1

This solves task 1 of the Atea assignment.

Some solution information:

- Uses Azure Functions with a TimerTrigger to run every 1 minutes
- Two separate projects, one for the Azure Functions and one for the REST API
- Both implemented with .NET 6 LTS
- REST API is minimal API
- REST API implements paging with continuation token (not very well tested)
- The code is commented a little more than I would normally do, but I wanted to make it easier to understand my reasoning
- No automatic testing is implemented

Some issues I ran into:

- I solved this on a relatively clean computer, takes time to setup tooling for local Azure development
- I didn't have experience with Azurite, so I had to spend some time figuring out how to use it compared to the Azure Storage Emulator
    - I ended up using Azurite, but running from CLI instead of implicitly as part of the Function. I am sure this can be done better, but I didn't have time to figure it out

Time spent: Approximately 4-5 hours. I spent a lot of time setting up the tooling and figuring out how to use Azurite. Most of the code is trivial, but I wanted to make sure it was well commented and easy to understand in this special case where I am trying to show my skills.

## Task 2

Some solution information:

- Uses ASP.NET with minimal API
- Registers a `BackgroundService` that runs every 1 minutes using a `PeriodicTimer`
- Stores cities in configuration using the options-pattern
- Uses fan-out parallelization for fetching weather data using HTTP
- Uses `System.Text.Json` for JSON serialization
- Uses EF Core with SQLite for storing weather data
- Uses Meteo as weather provider (might be a bad choice, but it was the first suitable one I found)
- I took some liberties and combined the datasets into one table, since I didn't see any reason to have them separate. They can be toggled by clicking in the header of the chart

![Screenshot of the chart](./content/Screenshot%202023-04-12%20102000.jpg)

Some issues I ran into:

- I have not used SQLite with EF Core before, so I had to spend some time figuring out how to use it
- The requirements are very vague, so I had to make some assumptions

Did not finish:

- In the alotted time, I couldn't make the chart respond to clicks in a meaningful way. I would have liked to have the chart show the data for the selected city, but I didn't have time to implement it

Time spend: Probably around 5-6 hours.

## Other improvements

- There is use of `HttpClient`, could conceivably have used something like Flurl instead
- Issues with the Meteo API:
    - It does not return cloud cover as part of `current_weather`. Seeing how it wasn't asked to chart it, I didn't include quering and parsing it in the model. This is a relatively easy fix, but I didn't have time to do it
    - It seems like `current_weather` is not updated very often, so the data is not very accurate. I have not found a way to get more accurate data out of Meteo. This results in the chart being very flat and a lot of the data being the same in the database
- I didn't know how to visualize the timestamp in the chart. I suppose it could be part of the tooltip, but I would love to have some feedback from product owner on it
- Probably 100 other things :)

Total time spent: Perhaps 10-11 hours.