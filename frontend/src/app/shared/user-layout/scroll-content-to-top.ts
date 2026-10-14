import {
  afterEveryRender, afterNextRender, DestroyRef, Directive,
  ElementRef, inject, Injector,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

@Directive({ selector: '[appScrollContentToTop]' })
export class ScrollContentToTop {
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private beforeAction: string | null = null;

  constructor() {
    // Capture the old content before the page's own click handler changes it.
    const onClick = (event: MouseEvent) => {
      const target = event.target;
      if (target instanceof Element && target.closest('button, [role="tab"]')) {
        this.beforeAction = this.element.innerHTML;
      }
    };
    this.element.addEventListener('click', onClick, true);
    this.destroyRef.onDestroy(() => this.element.removeEventListener('click', onClick, true));

    afterEveryRender(() => {
      const previous = this.beforeAction;
      this.beforeAction = null;
      if (previous !== null && previous !== this.element.innerHTML) {
        this.scrollToTop();
      }
    });

    inject(Router).events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => {
      afterNextRender(() => this.scrollToTop(), { injector: this.injector });
    });
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
