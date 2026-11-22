import { useEffect, useState } from "react";
import SvgIcon from "./SvgIcon";

export default function ScrollToTop({
  scrollContainerRef,
  visibilityThreshold = 260,
}) {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const container = scrollContainerRef?.current;

    if (!container) {
      setVisible(false);
      return undefined;
    }

    const handleScroll = () => {
      setVisible(container.scrollTop > visibilityThreshold);
    };

    handleScroll();

    container.addEventListener("scroll", handleScroll, {
      passive: true,
    });

    return () => {
      container.removeEventListener("scroll", handleScroll);
    };
  }, [scrollContainerRef, visibilityThreshold]);

  const handleScrollTop = () => {
    const container = scrollContainerRef?.current;

    if (!container) {
      return;
    }

    container.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  };

  if (!visible) {
    return null;
  }

  return (
    <button
      type="button"
      className="tp-scroll-top"
      onClick={handleScrollTop}
      title="Scroll to top"
      aria-label="Scroll to top"
    >
      <SvgIcon name="up" size={21} strokeColor="#ffffff" />
    </button>
  );
}
