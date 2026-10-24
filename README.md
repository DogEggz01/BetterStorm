# BetterStorm
*Disclaimer: This mod is made with ChatGPT 5.6 Sol*
### This mod have increased particles generation, it will affect performance on low spec computer
## Universal storm changes
- Storm wind bonus now start increasing at storm effect radius, and reach maximum at (storm core radius/2).
- Lighting now happen in the maximum storm wind area, so you can know approximately where it is.
- Lighting now spawn in circulus area, instead of vanilla square area.
- Enable Lighting light cause shadow.
- Increase the audio limit so now it can play more thunder sound in the same time.
- Increase Storm wind + ocean bonus limit to 40 (vanilla 20). Can be adjust by configurator.
- Gust modifier will only make wind stronger in storm.
- Gust last longer and wind speed change faster in storm.
- Change storm selection logic so it will pick closer storm for weather effect when multiple storm is nearby
- Change how rain particle generate so it look more consistant
- Introduce volume and rain size difference for different storm. (Overall louder)
- Add 4 custom storm. Have option to be compatible with Climate mod so it only show up in relevant season.
- Add Storm relocate distance slider in Configurator. You can increase it to reduce the chance you met storm (since this mod add more storm).
## Custom storm
- All custom storm can be toggle off seperately.
### squalls
- Very fast-moving super heavy rain storm. Very small area. There is 3 quall and each has different wind strength.
- These show up in all area and all season.
### Doldrum
- Weak wind area. When happen in Al'ankh and Aestrin, it present like high pressure system, no cloud, very little wind.
- When in Emerald and FFL area, it will still have it's cloud, but still little wind.
- Don't happen above 36N, only 1 copy so it's rare
### Magnetic Mist
- A light rain mist, the closer you're to the core, the thicker the mist and the cloud.
- As cloud getting thicker, it will block all the sunlight so you cannot use suncompass or chronocompass
  - You won't be able to see star and moon to do celestial navigation in the night.
- As you getting closer to core, your compass and chronometer will start swing around, until fully unusable when at core radius.
- Once the mist find you, it will follow you for 48 in game hour, then it will continued it's way
- There will be a 120 hour cooldown that it won't follow you again, but you can still sail into it.
- When you're affect by other storm, if the mist comes close it will still affect your navigation tools
- Sun totem can push the mist away temporally, it will come back to you after 6 in game hour.
- Magnetic mist only take effect when you're away from land atleast 9km (1 degree of lat/long)
  - It will also stop following you and start the 120 hour cooldown
## Regional special storm
- All regional special storm only spawn once. So it will be more rare.
- There is a toggle option to let all storm active regardless of season.
### Sandstorm
- Fast moving pile of sand but smaller radius. Bad visibility, everything yellow, your boat get dirty fast.
- Only active in Al'Ankh Sea. Show up in Spring/Summer if climate custom wind enable.
### Dry Thunderstorm
- Slow moving smaller storm, no rain, good visibility but a lot of bright lighting. Strong wind.
- Only active in Aestrin and Chronos sea. Show up in Autumn/Winter if climate custom wind enable.
### Hurricane
- Huge storm and little bit slower than vanilla storm. You can hear lighting from far away. Very strong wind, heavier rain.
- Only active in Emerald Archipelagos, Fire Fish Lagoon and Chronos Sea. Show up in Summer/Autumn if climate custom wind enable.
### Gentle Snow
- Small size slow moving "Storm". Clear weather, gentle wind, snow.
- Only active in north of 38N. Show up in Winter/Spring if Climate custom wind enable.
## Bug fix
- Fix thunder audio bug so it will play all 10 sound source. (Vanilla skip 2)
- Thunder sound actually comes from where lighting strike, instead of always from latest lighting strike position.
