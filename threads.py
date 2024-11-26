!pip install parsel
!pip install nested_lookup
!pip install playwright
# Install browser binaries
!playwright install
import json
from typing import Dict
import jmespath
from parsel import Selector
from nested_lookup import nested_lookup
from playwright.async_api import async_playwright
import asyncio

def parse_thread(data: Dict) -> str:
    """Parse Threads post JSON dataset for text content only"""
    result = jmespath.search("post.caption.text", data)
    return result

async def scrape_thread_text(url: str) -> dict:
    """Scrape Threads post and replies from a given URL, returning only text content"""
    async with async_playwright() as pw:
        browser = await pw.chromium.launch()
        context = await browser.new_context(viewport={"width": 1920, "height": 1080})
        page = await context.new_page()

        # go to url and wait for the page to load
        await page.goto(url)
        await page.wait_for_selector("[data-pressable-container=true]")

        # extract page content
        selector = Selector(await page.content())
        hidden_datasets = selector.css('script[type="application/json"][data-sjs]::text').getall()

        for hidden_dataset in hidden_datasets:
            if '"ScheduledServerJS"' not in hidden_dataset or "thread_items" not in hidden_dataset:
                continue
            data = json.loads(hidden_dataset)
            thread_items = nested_lookup("thread_items", data)
            if not thread_items:
                continue
            # Extract only the text content from the post and replies
            threads_text = [parse_thread(t) for thread in thread_items for t in thread]
            return {
                "thread_text": threads_text[0],
                "replies_text": threads_text[1:],
            }
        raise ValueError("could not find thread data in page")

# Run the async function and await it directly
await scrape_thread_text("https://www.threads.net/@chihan_ling")
