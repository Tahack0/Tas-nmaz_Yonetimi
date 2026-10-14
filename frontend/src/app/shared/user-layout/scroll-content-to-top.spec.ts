import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { ScrollContentToTop } from './scroll-content-to-top';

@Component({
  imports: [ScrollContentToTop],
  template: `
    <button class="outside" (click)="outside = !outside">Sidebar</button>
    <div appScrollContentToTop>
      <button class="toggle" (click)="open = !open">Toggle</button>
      <button class="unchanged">Download</button>
      @if (open) { <p>New content</p> }
    </div>`,
})
class TestPage {
  open = false;
  outside = false;
}

describe('ScrollContentToTop', () => {
  afterEach(() => vi.restoreAllMocks());

  it('scrolls after a content change, but ignores unchanged content and outside buttons', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const scroll = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    const fixture = TestBed.createComponent(TestPage);
    await fixture.whenStable();
    const click = async (selector: string) => {
      (fixture.nativeElement.querySelector(selector) as HTMLButtonElement).click();
      await fixture.whenStable();
    };
    await click('.toggle');
    expect(scroll).toHaveBeenCalledWith({ top: 0, behavior: 'smooth' });
    scroll.mockClear();
    await click('.unchanged');
    await click('.outside');
    expect(scroll).not.toHaveBeenCalled();
    await click('.toggle');
    expect(scroll).toHaveBeenCalledTimes(1);
  });
});
